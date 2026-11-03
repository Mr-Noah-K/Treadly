using UnityEditor;
using UnityEngine;

namespace Codex.LanRemoteInput.Editor
{
    internal static class LanRemoteInputMenu
    {
        [MenuItem("GameObject/LAN Remote Input/Receiver", false, 10)]
        private static void CreateReceiver(MenuCommand menuCommand)
        {
            CreateObject<LanInputReceiver>("LAN Input Receiver", menuCommand);
        }

        [MenuItem("GameObject/LAN Remote Input/Sender", false, 11)]
        private static void CreateSender(MenuCommand menuCommand)
        {
            CreateObject<LanInputSender>("LAN Input Sender", menuCommand);
        }

        private static void CreateObject<T>(string objectName, MenuCommand menuCommand) where T : Component
        {
            var gameObject = new GameObject(objectName, typeof(T));
            GameObjectUtility.SetParentAndAlign(gameObject, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {objectName}");
            Selection.activeObject = gameObject;
        }
    }
}
