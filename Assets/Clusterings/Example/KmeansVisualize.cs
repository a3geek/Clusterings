using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Clusterings.Example {
    public class KmeansVisualize : MonoBehaviour {
        public bool Calculating { get; private set; }

        private Kmeans kmeans = null;
        private List<Color> colors = new List<Color>();

        [SerializeField]
        private float wait = 1f;
        [SerializeField]
        private int nodes = 200;
        [SerializeField]
        private int clusters = 8;
        [SerializeField]
        private int initIteration = 10;
        [SerializeField]
        private float threshold = 0.001f;
        [SerializeField]
        private BoxCollider box = null;
        [SerializeField]
        private float nodeRadius = 1f;
        [SerializeField]
        private float clusterRadius = 1f;
        [Space]
        [SerializeField]
        private List<string> clusterViewer = new List<string>();


        void Awake() {
            var nodes = new List<Vector3>();

            var center = this.box.center;
            var size = this.box.size;
            for(var i = 0; i < this.nodes; i++) {
                nodes.Add(new Vector3(
                    Random.Range(center.x - size.x / 2f, center.x + size.x / 2f),
                    Random.Range(center.y - size.y / 2f, center.y + size.y / 2f),
                    Random.Range(center.x - size.z / 2f, center.z + size.z / 2f)
                ));
            }
            
            this.kmeans = new Kmeans(nodes, this.clusters, this.threshold, this.initIteration/*, this.box.center, this.box.size*/);
            this.Calculating = false;

            for(var i = 0; i < this.clusters; i++) {
                this.colors.Add(Random.ColorHSV());
            }
        }

        void Start() {
            StartCoroutine(this.Calculate());
        }

        private IEnumerator Calculate() {
            yield return new WaitForSeconds(this.wait);

            this.Calculating = true;
            yield return new WaitForSeconds(this.wait);

            while(this.kmeans.StepCalculate() == false) {
                yield return new WaitForSeconds(this.wait);
            }

            this.Calculating = false;
            Debug.Log("Finished");
        }
        
        void OnDrawGizmos() {
            if(Application.isPlaying == false || this.kmeans == null) {
                Gizmos.DrawSphere(this.box.center, this.nodeRadius);
                return;
            }

            var viewer = new List<int>();
            var clusters = this.kmeans.Clusters;

            for(var i = 0; i < clusters.Length; i++) {
                Gizmos.color = this.colors[i];
                Gizmos.DrawWireSphere(clusters[i].Centroid, this.clusterRadius);

                viewer.Add(0);
            }

            var nodes = this.kmeans.Nodes;
            for(var i = 0; i < nodes.Count; i++) {
                var n = nodes[i];
                Gizmos.color = this.colors[n.Cluster.ID];

                if(this.Calculating == true) {
                    Gizmos.DrawLine(n.point, n.Cluster.Centroid);
                }

                Gizmos.DrawSphere(n.point, this.nodeRadius);
                viewer[n.Cluster.ID]++;
            }

            this.clusterViewer = new List<string>();
            for(var i = 0; i < viewer.Count; i++) {
                this.clusterViewer.Add(i.ToString() + " : " + viewer[i].ToString());
            }
        }
    }
}
