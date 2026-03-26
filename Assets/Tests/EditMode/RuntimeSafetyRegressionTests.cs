using System.Reflection;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MakeItOut.Tests.EditMode
{
    public class RuntimeSafetyRegressionTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void TransparencyManager_UpdateTransparency_WhenMpbNull_DoesNotThrowAndLogsError()
        {
            GameObject go = new GameObject("TransparencyManager_Test");
            TransparencyManager manager = go.AddComponent<TransparencyManager>();

            FieldInfo mpbField = typeof(TransparencyManager).GetField("_mpb", PrivateInstance);
            FieldInfo listField = typeof(TransparencyManager).GetField("_currentlyTransparent", PrivateInstance);
            Assert.IsNotNull(mpbField);
            Assert.IsNotNull(listField);

            mpbField.SetValue(manager, null);
            listField.SetValue(manager, null);

            LogAssert.Expect(LogType.Error, "TransparencyManager: _mpb is null. Ensure Awake has run.");
            Assert.DoesNotThrow(() => manager.UpdateTransparency(Vector3Int.zero, Quaternion.identity));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FeaturePropRenderer_ValidateMaterials_LogsForInstancingDisabledMaterials()
        {
            GameObject go = new GameObject("FeaturePropRenderer_Test");
            FeaturePropRenderer renderer = go.AddComponent<FeaturePropRenderer>();

            Material ladder = new Material(Shader.Find("Standard")) { enableInstancing = false };
            Material stair = new Material(Shader.Find("Standard")) { enableInstancing = false };
            Material exit = new Material(Shader.Find("Standard")) { enableInstancing = false };

            SetPrivateField(renderer, "_ladderMaterial", ladder);
            SetPrivateField(renderer, "_stairMaterial", stair);
            SetPrivateField(renderer, "_exitMaterial", exit);

            LogAssert.Expect(LogType.Error, "FeaturePropRenderer: LadderMaterial does not have GPU instancing enabled.");
            LogAssert.Expect(LogType.Error, "FeaturePropRenderer: StairMaterial does not have GPU instancing enabled.");
            LogAssert.Expect(LogType.Error, "FeaturePropRenderer: ExitMaterial does not have GPU instancing enabled.");

            MethodInfo validateMethod = typeof(FeaturePropRenderer).GetMethod("ValidateMaterials", PrivateInstance);
            Assert.IsNotNull(validateMethod);
            validateMethod.Invoke(renderer, null);

            Object.DestroyImmediate(ladder);
            Object.DestroyImmediate(stair);
            Object.DestroyImmediate(exit);
            Object.DestroyImmediate(go);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.IsNotNull(field);
            field.SetValue(target, value);
        }
    }
}
