﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ThryPresetHandler {

    //variables for the presets
    private bool hasPresets = false;
    private bool presetsLoaded = false;
    private string presetsFilePath = null;
    private Dictionary<string, List<string[]>> presets = new Dictionary<string, List<string[]>>(); //presets

    //variabled for the preset selector
    public int selectedPreset = 0;
    private string[] presetOptions;
    string newPresetName = "Preset Name";

    public ThryPresetHandler(MaterialProperty[] props)
    {
        testPresetsChanged(props);
    }

    public ThryPresetHandler(MaterialProperty p)
    {
        testPresetsChanged(p);
    }

    public ThryPresetHandler(Shader s)
    {
        Material m = new Material(s);
        MaterialProperty[] props = MaterialEditor.GetMaterialProperties(new Material[] { m });
        testPresetsChanged(props);
    }

    //test if the path to the presets has changed
    public void testPresetsChanged(MaterialProperty[] props)
    {
        MaterialProperty presetsProperty = ThryEditor.FindProperty(props, "shader_presets");
        if (!(presetsProperty == null))
        {
            testPresetsChanged(presetsProperty);
        }
        else {
            hasPresets = false;
        }
    }

    //test if the path to the presets has changed
    public void testPresetsChanged(MaterialProperty p)
    {
        string newPath = stringToFilePath(p.displayName);
        if (presetsFilePath != newPath)
        {
            presetsFilePath = newPath;
            if (hasPresets) { loadPresets(); }
        }
    }

    //get the path to the presets from file name
    private string stringToFilePath(string name)
    {
        string[] guid = AssetDatabase.FindAssets(name, null);
        if (guid.Length > 0)
        {
            hasPresets = true;
            return AssetDatabase.GUIDToAssetPath(guid[0]);
        }
        return null;
    }

    public Dictionary<string, List<string[]>> getPresets()
    {
        return presets;
    }

    public bool shaderHasPresetPath()
    {
        return hasPresets;
    }

    //draws presets if exists
    public void drawPresets(MaterialProperty[] props, Material[] materials)
    {
        if (hasPresets && presetsLoaded)
        {
            int newSelectedPreset = EditorGUILayout.Popup(selectedPreset, presetOptions, GUILayout.MaxWidth(100));
            if (newSelectedPreset != selectedPreset)
            {
                selectedPreset = newSelectedPreset;
                if (selectedPreset < presetOptions.Length - 2) applyPreset(presetOptions[selectedPreset], props, materials);
                if (selectedPreset == presetOptions.Length - 2) ThryPresetEditor.open();
            }
            if (selectedPreset == presetOptions.Length - 1) drawNewPreset(props, materials);
        }
    }

    public void drawNewPreset(MaterialProperty[] props, Material[] materials)
    {
        newPresetName = GUILayout.TextField(newPresetName, GUILayout.MaxWidth(100));

        if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(20)))
        {
            addNewPreset(newPresetName, props, materials);
        }
    }

    //loads presets from file
    public void loadPresets()
    {
        presets.Clear();
        StreamReader reader = new StreamReader(presetsFilePath);
        string line;
        List<string[]> currentPreset = null;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length > 0 && !line.StartsWith("//"))
            {
                if (line.Contains("="))
                {
                    currentPreset.Add(line.Split(new string[] { " = " }, System.StringSplitOptions.None));
                }
                else
                {
                    currentPreset = new List<string[]>();
                    presets.Add(line, currentPreset);
                }
            }
        }
        reader.Close();
        presetOptions = new string[presets.Count + 3];
        presetOptions[0] = "Presets";
        presetOptions[presets.Count + 1] = " - Manage Presets -";
        presetOptions[presets.Count + 2] = "+ New +";
        int i = 1;
        foreach (string k in presets.Keys) presetOptions[i++] = k;
        presetsLoaded = true;
    }

    public List<string[]> getPropertiesOfPreset(string presetName)
    {
        List<string[]> returnList = new List<string[]>();
        presets.TryGetValue(presetName,out returnList);
        return returnList;
    }

    public void updatePresetProperty(string presetName, string property, string value)
    {
        List<string[]> properties = new List<string[]>();
        presets.TryGetValue(presetName,out properties);
        for (int i = 0; i < properties.Count; i++) if (properties[i][0] == property) properties[i][1] = value;
        this.savePresets();
    }

    public void removePresetProperty(string presetName, string property)
    {
        List<string[]> properties = new List<string[]>();
        presets.TryGetValue(presetName, out properties);
        for (int i = 0; i < properties.Count; i++) if (properties[i][0] == property) properties.RemoveAt(i);
        this.savePresets();
    }

    public void removePreset(string presetName)
    {
        presets.Remove(presetName);
        savePresets();
    }

    public void addNewPreset(string presetName)
    {
        presets.Add(presetName, new List<string[]>());
        savePresets();
    }

    public void addNewPreset(string name, MaterialProperty[] props, Material[] materials)
    {
        //find all non default values

        //add to presets list
        List<string[]> sets = new List<string[]>();

        foreach (MaterialProperty p in props)
        {
            string[] set = new string[] { p.name, "" };
            bool empty = false;
            Material defaultValues = new Material(materials[0].shader);
            switch (p.type)
            {
                case MaterialProperty.PropType.Float:
                case MaterialProperty.PropType.Range:
                    if (defaultValues != null && defaultValues.GetFloat(Shader.PropertyToID(set[0]))== p.floatValue) empty = true;
                    set[1] = "" + p.floatValue;
                    break;
                case MaterialProperty.PropType.Texture:
                    if (p.textureValue == null) empty = true;
                    else set[1] = "" + p.textureValue.name;
                    break;
                case MaterialProperty.PropType.Vector:
                    if (defaultValues != null && defaultValues.GetVector(Shader.PropertyToID(set[0])).Equals(p.vectorValue)) empty = true;
                    set[1] = "" + p.vectorValue.x + "," + p.vectorValue.y + "," + p.vectorValue.z + "," + p.vectorValue.w;
                    break;
                case MaterialProperty.PropType.Color:
                    if (defaultValues != null && defaultValues.GetColor(Shader.PropertyToID(set[0])).Equals(p.colorValue)) empty = true;
                    set[1] = "" + p.colorValue.r + "," + p.colorValue.g + "," + p.colorValue.b + "," + p.colorValue.a;
                    break;
            }
            if (p.flags != MaterialProperty.PropFlags.HideInInspector && !empty) sets.Add(set);
        }

        //fix all preset variables
        presets.Add(name, sets);
        string[] newPresetOptions = new string[presetOptions.Length + 1];
        for (int i = 0; i < presetOptions.Length; i++) newPresetOptions[i] = presetOptions[i];
        newPresetOptions[newPresetOptions.Length - 1] = presetOptions[newPresetOptions.Length - 2];
        newPresetOptions[newPresetOptions.Length - 2] = name;
        presetOptions = newPresetOptions;
        newPresetName = "Preset Name";

        //save all presets into file
        savePresets();
    }

    public void setPreset(string presetName, List<string[]> list)
    {
        presets.Remove(presetName);
        presets.Add(presetName, list);
        savePresets();
    }

    private void savePresets()
    {
        StreamWriter writer = new StreamWriter(presetsFilePath, false);
        foreach (KeyValuePair<string, List<string[]>> preset in presets)
        {
            writer.WriteLine(preset.Key);
            foreach (string[] set in preset.Value) writer.WriteLine(set[0] + " = " + set[1]);
            writer.WriteLine("");
        }
        writer.Close();
    }

    public void applyPreset(string presetName, MaterialProperty[] props, Material[] materials)
    {
        List<string[]> sets;
        if (presets.TryGetValue(presetName, out sets))
        {
            foreach (string[] set in sets)
            {
                MaterialProperty p = ThryEditor.FindProperty(props, set[0]);
                if (p != null)
                {
                    if (p.type == MaterialProperty.PropType.Texture)
                    {
                        string[] guids = AssetDatabase.FindAssets(set[1] + " t:Texture", null);
                        if (guids.Length == 0) Debug.LogError("Couldn't find texture: " + set[1]);
                        else
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                            Texture tex = (Texture)EditorGUIUtility.Load(path);
                            foreach(Material m in materials) m.SetTexture(Shader.PropertyToID(set[0]), tex);
                        }
                    }
                    else if (p.type == MaterialProperty.PropType.Float || p.type == MaterialProperty.PropType.Range)
                    {
                        float value;
                        if (float.TryParse(set[1], out value)) foreach (Material m in materials) m.SetFloat(Shader.PropertyToID(set[0]), value);
                    }
                    else if (p.type == MaterialProperty.PropType.Vector)
                    {
                        string[] xyzw = set[1].Split(",".ToCharArray());
                        Vector4 vector = new Vector4(float.Parse(xyzw[0]), float.Parse(xyzw[1]), float.Parse(xyzw[2]), float.Parse(xyzw[3]));
                        foreach (Material m in materials) m.SetVector(Shader.PropertyToID(set[0]), vector);
                    }
                    else if (p.type == MaterialProperty.PropType.Color)
                    {
                        float[] rgba = new float[4];
                        string[] rgbaString = set[1].Split(',');
                        float.TryParse(rgbaString[0], out rgba[0]);
                        float.TryParse(rgbaString[1], out rgba[1]);
                        float.TryParse(rgbaString[2], out rgba[2]);
                        if (rgbaString.Length > 3) float.TryParse(rgbaString[3], out rgba[3]); else rgba[3] = 1;
                        foreach (Material m in materials) m.SetColor(Shader.PropertyToID(set[0]), new Color(rgba[0], rgba[1], rgba[2], rgba[3]));
                    }
                }else if (set[0] == "render_queue")
                {
                    int q = 0;
                    Debug.Log(set[0] + "," + set[1]);
                    if(int.TryParse(set[1],out q)){
                        foreach (Material m in materials) m.renderQueue = q;
                    }
                }
            }
        }
    }
}
