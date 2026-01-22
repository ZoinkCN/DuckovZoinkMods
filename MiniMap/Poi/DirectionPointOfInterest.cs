using MiniMap.Utils;
using UnityEngine;

namespace MiniMap.Poi
{
    public class DirectionPointOfInterest : CharacterPoiBase
    {
        private float rotationEulerAngle;
        private float baseEulerAngle;

        public float RotationEulerAngle { get => rotationEulerAngle % 360; set => rotationEulerAngle = value % 360; }
        public float BaseEulerAngle { get => baseEulerAngle % 360; set => baseEulerAngle = value % 360; }
        public float RealEulerAngle => (baseEulerAngle + rotationEulerAngle) % 360;
        public override string DisplayName => string.Empty;
        public override bool IsArea => false;
        public override float AreaRadius => 0;
        public override Color Color => Color.white;

        protected override void Update()
        {
            if (Character == null || CharacterType != CharacterType.Main && PoiCommon.IsDead(Character))
            {
                Destroy(this.gameObject);
                return;
            }
            base.Update();
            RotationEulerAngle = MiniMapCommon.GetChracterRotation(Character);
        }
    }
}