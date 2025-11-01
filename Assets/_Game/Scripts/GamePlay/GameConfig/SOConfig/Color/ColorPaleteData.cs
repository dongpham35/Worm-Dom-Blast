using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorPaleteData", menuName = "ScriptableObject/ColorConfig/ColorPaleteData")]
public class ColorPaleteData : ScriptableObject
{
    [SerializedDictionary("Color Name", "Color")]
    public SerializedDictionary<string, Color> ColorPalete;
    #region SINGLETON PATTERN

    private static ColorPaleteData _instance;
    public static ColorPaleteData Instance
    {
        get
        {
            if (_instance == null)
            {
                // Tìm asset ScriptableObject trong Resources folder
                _instance = Resources.Load<ColorPaleteData>("ConfigSO/ColorPaleteData");

                // Cảnh báo nếu chưa có asset
                if (_instance == null)
                {
                    Debug.LogError("Không tìm thấy ColorPaleteData.asset trong thư mục Resources!");
                }
            }
            return _instance;
        }
    }
    #endregion
}
