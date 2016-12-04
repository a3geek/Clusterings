using UnityEngine;

namespace Clusterings {
    public class Node {
        public Vector3 point { get; private set; }
        public Cluster Cluster { get; private set; }

        
        public Node(Vector3 point, Cluster cluster) {
            this.point = point;
            this.Cluster = cluster;
        }
    }

    public class Cluster {
        public int ID { get; private set; }
        public Vector3 Centroid { get; private set; }


        public Cluster(int id, Vector3 centroid) {
            this.ID = id;
            this.Centroid = centroid;
        }
    }
}
