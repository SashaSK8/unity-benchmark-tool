using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Benchmark
{
    public abstract class MaterialPropertyGUI
    {
        protected string name;
        protected int id;
        protected Material material;

        public MaterialPropertyGUI(Material material, string name, int id)
        {
            this.name = name;
            this.id = id;
            this.material = material;
        }

        public abstract void OnGUI();
    }

    public class FloatRangePropertyGUI : MaterialPropertyGUI
    {
        private float value;
        private Vector2 range;

        public FloatRangePropertyGUI(Material material, string name, int id) : base(material, name, id)
        {
            value = material.GetFloat(id);
            range = material.shader.GetPropertyRangeLimits(material.shader.FindPropertyIndex(name));
        }

        public override void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.ExpandWidth(false));
            GUILayout.Label($"{value:0.00}", GUILayout.ExpandWidth(false));
            var input = GUILayout.HorizontalSlider(value, range.x, range.y);
            if (input != value)
            {
                value = input;
                material.SetFloat(id, value);
            }
            GUILayout.EndHorizontal();
        }
    }

    public class FloatPropertyGUI : MaterialPropertyGUI
    {
        private float value;

        public FloatPropertyGUI(Material material, string name, int id) : base(material, name, id)
        {
            value = material.GetFloat(id);
        }

        public override void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name);
            if (float.TryParse(GUILayout.TextField(value.ToString()), out var f))
            {
                if (value != f)
                {
                    value = f;
                    material.SetFloat(id, f);
                }
            }
            GUILayout.EndHorizontal();
        }
    }

    public class MaterialGUI
    {
        private readonly List<MaterialPropertyGUI> properties = new List<MaterialPropertyGUI>();
        private readonly Material material;
        public Material Material => material;

        public MaterialGUI(Material material)
        {
            this.material = material;
            var shader = material.shader;
            for (int i = 0, count = shader.GetPropertyCount(); i < count; i++)
            {
                var name = shader.GetPropertyName(i);
                var id = shader.GetPropertyNameId(i);
                switch (shader.GetPropertyType(i))
                {
                    case ShaderPropertyType.Range:
                        properties.Add(new FloatRangePropertyGUI(material, name, id));
                        break;
                    case ShaderPropertyType.Float:
                        properties.Add(new FloatPropertyGUI(material, name, id));
                        break;
                }
            }
        }

        public void OnGUI()
        {
            foreach (var prop in properties)
                prop.OnGUI();
        }
    }

    public static class GUIUtils
    {
        public static void Slider(string name, float min, float max, ref float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{name} {value:0.00}", GUILayout.ExpandWidth(false));
            GUILayout.Space(4);
            value = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.EndHorizontal();
        }

        public static void IntField(string name, ref int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name);
            if (!int.TryParse(GUILayout.TextField(value.ToString()), out value))
                value = 1;
            GUILayout.EndHorizontal();
        }

        public static void FloatField(string name, ref float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name);
            if (!float.TryParse(GUILayout.TextField($"{value:0.00}"), out value))
                value = 1;
            if (GUILayout.Button("-", GUILayout.Width(25))) value -= 0.5f;
            if (GUILayout.Button("+", GUILayout.Width(25))) value += 0.5f;
            GUILayout.EndHorizontal();
        }

        public static void Vector3IntField(string name, ref Vector3Int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name);
            GUILayout.Label("x", GUILayout.ExpandWidth(false));
            value.x = int.TryParse(GUILayout.TextField(value.x.ToString()), out var x) ? x : 1;
            GUILayout.Label("y", GUILayout.ExpandWidth(false));
            value.y = int.TryParse(GUILayout.TextField(value.y.ToString()), out var y) ? y : 1;
            GUILayout.Label("z", GUILayout.ExpandWidth(false));
            value.z = int.TryParse(GUILayout.TextField(value.z.ToString()), out var z) ? z : 1;
            GUILayout.EndHorizontal();
        }
    }
}
