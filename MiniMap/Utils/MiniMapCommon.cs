using Duckov.MiniMaps;
using Duckov.MiniMaps.UI;
using HarmonyLib;
using MiniMap.Managers;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniMap.Utils
{
    public static class MiniMapCommon
    {
        public const float originMapZRotation = -30f;

        //private static Vector3 GetCharacterForward(CharacterMainControl? character)
        //{
        //    if (character == null)
        //    {
        //        return Vector3.zero;
        //    }
        //    return LevelManager.Instance.InputManager.InputAimPoint - CharacterMainControl.Main.transform.position;
        //}

        //public static Vector3 GetPlayerMinimapGlobalPosition(MiniMapDisplay minimapDisplay)
        //{
        //    Vector3 vector;
        //    var sceneID = SceneInfoCollection.GetSceneID(SceneManager.GetActiveScene().buildIndex);
        //    minimapDisplay.TryConvertWorldToMinimap(LevelManager.Instance.MainCharacter.transform.position, sceneID, out vector);
        //    return minimapDisplay.transform.localToWorldMatrix.MultiplyPoint(vector);
        //}

        private static float GetAngle()
        {
            Vector3 to = LevelManager.Instance.InputManager.InputAimPoint - CharacterMainControl.Main.transform.position;
            return Vector3.SignedAngle(Vector3.forward, to, Vector3.up);
        }

        //public static Quaternion GetChracterRotation(Vector3 to)
        //{
        //    float currentMapZRotation =
        //    return Quaternion.Euler(0f, 0f, -currentMapZRotation);
        //}

        public static Quaternion GetChracterRotation()
        {
            CharacterMainControl? character = CharacterMainControl.Main;
            if (character == null)
            {
                return Quaternion.Euler(0, 0, 0);
            }
            string facingBase = ModSettingManager.GetActualDropdownValue("facingBase", false);
            if (facingBase == "Mouse")
            {
                return Quaternion.Euler(0f, 0f, -GetAngle());
            }
            else
            {
                float rotationEulerAngle = character.transform.Find("ModelRoot").rotation.eulerAngles.y;
                return Quaternion.Euler(0, 0, rotationEulerAngle);
            }
        }

        //public static Quaternion GetPlayerMinimapRotationInverse(Vector3 to)
        //{
        //    float currentMapZRotation = Vector3.SignedAngle(Vector3.forward, to, Vector3.up);
        //    return Quaternion.Euler(0f, 0f, currentMapZRotation);
        //}

        public static Quaternion GetPlayerMinimapRotationInverse()
        {
            return Quaternion.Euler(0f, 0f, GetAngle());
        }
    }
}