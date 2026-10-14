using UnityEngine;
namespace TwinkleOps
{
    public sealed class InfectedAppearance : MonoBehaviour
    {
        public SkinnedMeshRenderer skin;
        public Mesh[] silhouettes;
        public Material[] outfits;
        public int variant;
        void Awake()=>Apply(variant);
        public void Apply(int index)
        {
            variant=index;if(!skin||silhouettes==null||silhouettes.Length==0)return;
            skin.sharedMesh=silhouettes[index%silhouettes.Length];
            if(outfits!=null&&outfits.Length>0)skin.sharedMaterial=outfits[index%outfits.Length];
        }
    }
}
