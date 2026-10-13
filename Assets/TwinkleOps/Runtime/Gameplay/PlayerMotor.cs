using UnityEngine;
using UnityEngine.InputSystem;

namespace TwinkleOps
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public Camera view;
        public float walkSpeed=4, sprintSpeed=6.3f, sensitivity=.09f;
        public bool InputEnabled=true;
        public Vector2 MoveInput { get; private set; }
        public bool FireHeld, AimHeld, ReloadPressed, InteractPressed, BagPressed, SwitchPressed;
        public int WeaponSelected=-1;
        CharacterController controller;
        float pitch, vertical;
        bool useTouch;
        readonly MobileInput touch=new();
        void Awake() { controller=GetComponent<CharacterController>(); useTouch=Application.isMobilePlatform; }
        public void AddRecoil(float amount) { pitch=Mathf.Clamp(pitch-amount,-85,85); }
        void Update()
        {
            FireHeld=AimHeld=ReloadPressed=InteractPressed=BagPressed=SwitchPressed=false; WeaponSelected=-1;
            var key=Keyboard.current; var mouse=Mouse.current;
            BagPressed=key?.tabKey.wasPressedThisFrame==true;
            if(Touchscreen.current!=null && Touchscreen.current.primaryTouch.press.isPressed) useTouch=true;
            touch.Read(); BagPressed |= touch.bag;
            if(!InputEnabled) { MoveInput=Vector2.zero; return; }
            if(key?.escapeKey.wasPressedThisFrame==true) { InputEnabled=false; return; }
            Vector2 move=Vector2.zero, look=Vector2.zero;
            if(key!=null)
            {
                move=new Vector2((key.dKey.isPressed?1:0)-(key.aKey.isPressed?1:0),(key.wKey.isPressed?1:0)-(key.sKey.isPressed?1:0));
                ReloadPressed=key.rKey.wasPressedThisFrame; InteractPressed=key.eKey.wasPressedThisFrame;
                SwitchPressed=key.qKey.wasPressedThisFrame;
                if(key.digit1Key.wasPressedThisFrame) WeaponSelected=0;
                if(key.digit2Key.wasPressedThisFrame) WeaponSelected=1;
            }
            if(mouse!=null && Cursor.lockState==CursorLockMode.Locked)
            { look=mouse.delta.ReadValue()*sensitivity; FireHeld=mouse.leftButton.isPressed; AimHeld=mouse.rightButton.isPressed; }
            if(Touchscreen.current!=null)
            { move+=touch.move; look+=touch.look*.13f; FireHeld|=touch.fire; AimHeld|=touch.aim; ReloadPressed|=touch.reload; InteractPressed|=touch.interact; SwitchPressed|=touch.switchWeapon; }
            MoveInput=Vector2.ClampMagnitude(move,1);
            transform.Rotate(Vector3.up*look.x); pitch=Mathf.Clamp(pitch-look.y,-85,85); view.transform.localEulerAngles=new(pitch,0,0);
            if(controller.isGrounded && vertical<0) vertical=-2;
            if(controller.isGrounded && key?.spaceKey.wasPressedThisFrame==true) vertical=5;
            vertical-=18*Time.deltaTime;
            float speed=key?.leftShiftKey.isPressed==true && !AimHeld?sprintSpeed:walkSpeed;
            controller.Move((transform.TransformDirection(new Vector3(MoveInput.x,0,MoveInput.y))*speed+Vector3.up*vertical)*Time.deltaTime);
        }
        public void DrawTouch() { if(useTouch && Touchscreen.current!=null && InputEnabled) touch.Draw(); }
    }

    // Screen fractions keep controls usable after orientation/viewport changes. Buttons are multi-touch.
    sealed class MobileInput
    {
        public Vector2 move,look;
        public bool fire,aim,reload,interact,bag,switchWeapon;
        int moveId=-1,lookId=-1;
        Vector2 origin;
        static Rect Fire=>new(.84f,.53f,.15f,.19f);
        static Rect Aim=>new(.68f,.65f,.13f,.15f);
        static Rect Reload=>new(.84f,.76f,.15f,.12f);
        static Rect Interact=>new(.68f,.82f,.13f,.12f);
        static Rect Bag=>new(.85f,.03f,.13f,.1f);
        static Rect Switch=>new(.68f,.03f,.13f,.1f);
        public void Read()
        {
            move=look=Vector2.zero; fire=aim=reload=interact=bag=switchWeapon=false;
            if(Touchscreen.current==null) return;
            bool hasMove=false,hasLook=false;
            foreach(var t in Touchscreen.current.touches)
            {
                if(!t.press.isPressed) continue;
                int id=t.touchId.ReadValue(); Vector2 pos=t.position.ReadValue();
                Vector2 n=new(pos.x/Screen.width,1-pos.y/Screen.height);
                if(Fire.Contains(n)) { fire=true; continue; }
                if(Aim.Contains(n)) { aim=true; continue; }
                if(Reload.Contains(n)) { reload|=t.press.wasPressedThisFrame; continue; }
                if(Interact.Contains(n)) { interact|=t.press.wasPressedThisFrame; continue; }
                if(Bag.Contains(n)) { bag|=t.press.wasPressedThisFrame; continue; }
                if(Switch.Contains(n)) { switchWeapon|=t.press.wasPressedThisFrame; continue; }
                if(t.press.wasPressedThisFrame)
                { if(n.x<.45f && moveId<0) { moveId=id; origin=pos; } else if(n.x>=.45f && lookId<0) lookId=id; }
                if(id==moveId) { hasMove=true; move=Vector2.ClampMagnitude((pos-origin)/(Screen.height*.12f),1); }
                if(id==lookId) { hasLook=true; look=t.delta.ReadValue(); }
            }
            if(!hasMove) moveId=-1; if(!hasLook) lookId=-1;
        }
        public void Draw()
        {
            Label(Fire,"开火"); Label(Aim,"瞄准"); Label(Reload,"换弹"); Label(Interact,"交互"); Label(Bag,"背包"); Label(Switch,"切枪");
            Label(new Rect(.02f,.7f,.23f,.23f),"左侧移动");
        }
        static void Label(Rect r,string text) => GUI.Box(new Rect(r.x*Screen.width,r.y*Screen.height,r.width*Screen.width,r.height*Screen.height),text);
    }
}
