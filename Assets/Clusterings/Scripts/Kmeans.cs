using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Random = UnityEngine.Random;

namespace Clusterings {
    public sealed class Kmeans {
        public List<Node> Nodes {
            get { return this.nodes.ConvertAll(e => e.GetNode(this.clusters[e.ClusterID].Centroid)); }
        }
        public Cluster[] Clusters {
            get { return this.clusters.ToList().ConvertAll(e => (Cluster)e).ToArray(); }
        }
        public int NumberOfCluster {
            get { return this.clusters.Length; }
        }
        public float Threshold { get; private set; }
        public bool Finished {
            get { return this.finished <= 0; }
        }

        private bool step = true;
        private int finished = 2;
        private List<InternalNode> nodes = new List<InternalNode>();
        private InternalCluster[] clusters = new InternalCluster[0];


        public Kmeans(List<Vector3> nodes, int clusters, float threshold) {
            this.Init(nodes, clusters, threshold);
            this.InitCalculate(clusters);
            this.step = false;
        }

        public Kmeans(List<Vector3> nodes, int clusters, float threshold, int iteration) {
            this.Init(nodes, clusters, threshold);
            this.InitCalculate(clusters, iteration);
        }

        public Kmeans(List<Vector3> nodes, int clusters, float threshold, int iteration, Vector3 center, Vector3 size) {
            this.Init(nodes, clusters, threshold);
            this.InitCalculate(clusters, iteration, center, size);
        }
        
        public void Calculate() {
            while(this.StepCalculate() == false) {; }
        }
        
        public bool StepCalculate() {
            if(this.Finished) { return true; }

            this.finished--;
            this.step = !this.step;

            if(this.step) {
                if(this.CalculateClustering()) { this.finished++; }
            }
            else {
                if(this.CalculateCentroid() == false) { this.finished++; }
            }
            
            return this.Finished;
        }

        private void Init(List<Vector3> nodes, int clusters, float threshold) {
            this.Threshold = threshold;

            this.clusters = new InternalCluster[clusters];
            for(var i = 0; i < clusters; i++) {
                this.clusters[i] = new InternalCluster(i, Vector3.zero);
            }

            this.nodes = nodes.ConvertAll(node => new InternalNode(node, 0));
        }

        private void InitCalculate(int numberOfCluster) {
            /*
             * データ群全てをランダムにクラスタリングして、各クラスタの重心を計算し初期重心とする
             */
            for(var i = 0; i < this.nodes.Count; i++) {
                this.nodes[i].ClusterID = i < numberOfCluster ? i : Random.Range(0, numberOfCluster);
            }

            this.CalculateCentroid();
        }

        private void InitCalculate(int numberOfCluster, int iteration) {
            /*
             * データ群の中から適当にN個抜いてきて重心とする
             */
            var clustered = new List<int>();

            this.InitIterativeCalculate(numberOfCluster, iteration, index => {
                clustered = new List<int>();

                while(clustered.Count < numberOfCluster) {
                    var rand = Random.Range(0, nodes.Count);

                    if(clustered.Contains(rand) == false) {
                        clustered.Add(rand);
                    }
                }
            }, index => this.nodes[clustered[index]].Point);
        }

        private void InitCalculate(int numberOfCluster, int iteration, Vector3 center, Vector3 size) {
            /*
             * 空間内にランダムな座標をN個作って、それを重心とする
             */
            this.InitIterativeCalculate(numberOfCluster, iteration, null, index => {
                var half = size / 2f;

                return new Vector3(
                    Random.Range(center.x - half.x, center.x + half.x / 2f),
                    Random.Range(center.y - half.y / 2f, center.y + half.y / 2f),
                    Random.Range(center.z - half.z / 2f, center.z + half.z / 2f)
                );
            });
        }

        private void InitIterativeCalculate(int numberOfCluster, int iteration, Action<int> iterativeCalculate, Func<int, Vector3> calculateCentroid) {
            var clustersList = new List<KeyValuePair<float, InternalCluster[]>>();

            for(var i = 0; i < iteration; i++) {
                if(iterativeCalculate != null) { iterativeCalculate(i); }
                var clusters = new InternalCluster[numberOfCluster];
                
                for(var j = 0; j < this.clusters.Length; j++) {
                    clusters[j] = new InternalCluster(j, calculateCentroid(j));
                    this.clusters[j] = clusters[j];
                }
                this.CalculateClustering();

                // sse ... クラスタ内誤差総和
                var sse = 0f;
                this.nodes.ForEach(node => sse += (node.Point - this.clusters[node.ClusterID].Centroid).sqrMagnitude);

                clustersList.Add(new KeyValuePair<float, InternalCluster[]>(sse, clusters));
            }

            this.clusters = clustersList.OrderBy(e => e.Key).First().Value;
            this.CalculateClustering();
        }
        
        private bool CalculateClustering() {
            var finished = true;

            for(var i = 0; i < this.nodes.Count; i++) {
                var node = this.nodes[i];
                var distance = new List<KeyValuePair<int, float>>();

                for(var j = 0; j < this.clusters.Length; j++) {
                    if(node.ClusterID == j) { continue; }

                    var current = (node.Point - this.clusters[node.ClusterID].Centroid).sqrMagnitude;
                    var other = (node.Point - this.clusters[j].Centroid).sqrMagnitude;

                    if(current > other) {
                        distance.Add(new KeyValuePair<int, float>(j, current - other));
                    }
                }

                if(distance.Count > 0) {
                    node.ClusterID = distance.OrderBy(e => e.Value).Last().Key;
                    finished = false;
                }
            }

            return finished;
        }

        private bool CalculateCentroid() {
            var finish = true;

            for(var i = 0; i < this.clusters.Length; i++) {
                var clusterNodes = this.nodes.FindAll(node => node.ClusterID == i);
                
                var sum = Vector3.zero;
                clusterNodes.ForEach(e => sum += e.Point);

                var previous = this.clusters[i].Centroid;
                this.clusters[i].Centroid = sum / clusterNodes.Count;

                var sqrMag = (previous - this.clusters[i].Centroid).sqrMagnitude;
                if(sqrMag > this.Threshold * this.Threshold) {
                    finish = false;
                }
            }

            return finish;
        }
        
        private class InternalNode {
            public Vector3 Point = Vector3.zero;
            public int ClusterID = 0;

            
            public InternalNode(Vector3 point, int cluster) {
                this.Point = point;
                this.ClusterID = cluster;
            }

            public Node GetNode(Vector3 centroid) {
                return new Node(this.Point, new Cluster(this.ClusterID, centroid));
            }
        }

        private class InternalCluster {
            public int ID = 0;
            public Vector3 Centroid = Vector3.zero;


            public InternalCluster(int id, Vector3 centroid) {
                this.ID = id;
                this.Centroid = centroid;
            }

            public static implicit operator Cluster(InternalCluster cluster) {
                return new Cluster(cluster.ID, cluster.Centroid);
            }
        }
    }
}
