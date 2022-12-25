namespace Bookmark4Unity.Editor
{
    using System;

    [Serializable]
    public class AssetData : IEquatable<AssetData>
    {
        public string guid;
        public string path;
        public string name;
        public string type;

        public override bool Equals(object obj) => this.Equals(obj as AssetData);

        public bool Equals(AssetData other)
        {
            if (other is null) return false;
            if (System.Object.ReferenceEquals(this, other)) return true;
            return this.guid == other.guid;
        }

        public override int GetHashCode() => guid.GetHashCode();

        public static bool operator ==(AssetData lhs, AssetData rhs)
        {
            if (lhs is null)
            {
                if (rhs is null) return true;
                return false;
            }

            return lhs.Equals(rhs);
        }

        public static bool operator !=(AssetData lhs, AssetData rhs) => !(lhs == rhs);
    }
}