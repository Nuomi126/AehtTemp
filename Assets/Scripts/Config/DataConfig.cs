// DataConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Data Config")]
public class DataConfig : ScriptableObject
{
    public string csvResourcePath = "Data/qhwc";
    public string databaseAPIUrl;
    public string apiKey;
}