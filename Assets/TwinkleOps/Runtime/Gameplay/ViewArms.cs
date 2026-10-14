using System.Linq;
using UnityEngine;

namespace TwinkleOps
{
    // Poses the licensed skinned arm mesh with a two-bone solve. No visible proxy geometry.
    [DefaultExecutionOrder(200)]
    public sealed class ViewArms : MonoBehaviour
    {
        public Rifle rifle;
        Transform[] bones;
        Vector3[] positions;
        Quaternion[] rotations;
        void Awake() => Cache();
        void Cache()
        {
            bones=GetComponentsInChildren<Transform>(); positions=bones.Select(b=>b.localPosition).ToArray(); rotations=bones.Select(b=>b.localRotation).ToArray();
        }
        Transform Bone(string name) => bones.FirstOrDefault(b=>b.name==name);
        void LateUpdate() => Pose();
        public void Pose()
        {
            if(!rifle || !rifle.weaponRoot) return;
            if(bones==null) Cache();
            for(int i=1;i<bones.Length;i++) { bones[i].localPosition=positions[i]; bones[i].localRotation=rotations[i]; }
            var cam=rifle.motor.view.transform; var gun=rifle.weaponRoot;
            Vector3 right=gun.position+cam.TransformVector(new Vector3(.015f,-.055f,-.12f));
            Vector3 left=gun.position+cam.TransformVector(new Vector3(-.035f,-.045f,.16f));
            if(rifle.IsMelee)
            {
                right=gun.position+cam.TransformVector(new Vector3(0,-.015f,-.11f));
                left=cam.TransformPoint(new Vector3(-.23f,-.33f,.29f));
            }
            if(rifle.Reloading) left+=cam.TransformVector(new Vector3(.02f,-.13f,-.12f));
            Solve("R",right,cam.right-cam.up,cam);
            Solve("L",left,-cam.right-cam.up,cam);
        }
        void Solve(string side,Vector3 target,Vector3 bend,Transform cam)
        {
            var upper=Bone("upper_arm."+side); var fore=Bone("forearm."+side); var end=Bone("forearm."+side+"_end"); var hand=Bone("hand."+side);
            if(!upper || !fore || !end || !hand) return;
            float a=Vector3.Distance(upper.position,fore.position), b=Vector3.Distance(fore.position,end.position);
            var delta=target-upper.position; float d=Mathf.Clamp(delta.magnitude,.02f,a+b-.005f); var dir=delta.normalized;
            var pole=Vector3.ProjectOnPlane(bend,dir).normalized;
            float along=(a*a+d*d-b*b)/(2*d), height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            var elbow=upper.position+dir*along+pole*height;
            upper.rotation=Quaternion.FromToRotation(fore.position-upper.position,elbow-upper.position)*upper.rotation;
            fore.rotation=Quaternion.FromToRotation(end.position-fore.position,target-fore.position)*fore.rotation;
            hand.position=target;
            var finger=Bone("f_middle.01."+side);
            if(finger) hand.rotation=Quaternion.FromToRotation(finger.position-hand.position,cam.forward)*hand.rotation;
            foreach(var bone in bones)
            {
                if(bone.name.StartsWith("f_") && bone.name.EndsWith("."+side)) bone.localRotation*=Quaternion.Euler(60,0,0);
                if(bone.name.StartsWith("thumb.") && bone.name.EndsWith("."+side)) bone.localRotation*=Quaternion.Euler(18,0,0);
            }
        }
    }
}
