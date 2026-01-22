using Duckov.MiniMaps;
using Duckov.MiniMaps.UI;
using HarmonyLib;
using MiniMap.Managers;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniMap.Utils
{
    public static class MiniMapCommon
    {
        public const float originMapZRotation = -30f;

        private static float GetAngle()
        {
            Vector3 to = LevelManager.Instance.InputManager.InputAimPoint - CharacterMainControl.Main.transform.position;
            return Vector3.SignedAngle(Vector3.forward, to, Vector3.up);
        }

        public static float GetChracterRotation(CharacterMainControl? character)
        {
            if (character == null)
            {
                return 0;
            }
            string facingBase = ModSettingManager.GetActualDropdownValue("facingBase", false);
            if (character.IsMainCharacter && facingBase == "Mouse")
            {
                return -GetAngle();
            }
            else
            {
                return -character.modelRoot.rotation.eulerAngles.y;
            }
        }

        public static float GetMinimapRotation()
        {
            return -GetChracterRotation(CharacterMainControl.Main);
        }
    }
}