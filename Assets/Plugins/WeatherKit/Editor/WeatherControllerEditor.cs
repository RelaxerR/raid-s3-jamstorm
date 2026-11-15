#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeatherController))]
public class WeatherControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Погода (переключение в Play Mode)", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            DrawButton("Ясно", WeatherPreset.Clear);
            DrawButton("Облачно", WeatherPreset.Cloudy);
            DrawButton("Дождь", WeatherPreset.Rain);
            DrawButton("Гроза", WeatherPreset.Storm);
            DrawButton("Снег", WeatherPreset.Snow);
            DrawButton("Туман", WeatherPreset.Fog);
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Запустите Play Mode, затем нажимайте кнопки, чтобы менять погоду во время игры.", MessageType.Info);
    }

    private void DrawButton(string label, WeatherPreset preset)
    {
        if (GUILayout.Button(label))
            ((WeatherController)target).SetWeather(preset);
    }
}
#endif
