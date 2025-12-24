using System.Globalization;
using System.Resources;
using ChronoView.Resources;

namespace ChronoView.Core.Localization;

/// <summary>
/// 중앙화된 로컬라이징 관리자
/// 모든 UI 문자열을 리소스 파일에서 가져옵니다.
/// </summary>
public static class LocalizationManager
{
    private static readonly ResourceManager ResourceManager = Strings.ResourceManager;
    private static CultureInfo _currentCulture = CultureInfo.CurrentCulture;

    /// <summary>
    /// 현재 문화권을 설정합니다.
    /// </summary>
    public static void SetCulture(CultureInfo culture)
    {
        _currentCulture = culture;
    }

    /// <summary>
    /// 리소스 키로 문자열을 가져옵니다.
    /// </summary>
    public static string GetString(string key)
    {
        return ResourceManager.GetString(key, _currentCulture) ?? key;
    }

    /// <summary>
    /// 포맷 문자열을 가져옵니다.
    /// </summary>
    public static string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        return string.Format(format, args);
    }

    // 편의 메서드들

    /// <summary>
    /// 데이터 타입에 따른 컬럼 헤더를 가져옵니다.
    /// </summary>
    public static string GetColumnHeader(Models.DataType dataType)
    {
        return dataType switch
        {
            Models.DataType.Normal => GetString("Column_Normal"),
            Models.DataType.NIR => GetString("Column_NIR"),
            Models.DataType.Cam1 => GetString("Column_Cam1"),
            Models.DataType.Cam2 => GetString("Column_Cam2"),
            Models.DataType.Cam3 => GetString("Column_Cam3"),
            Models.DataType.Cam4 => GetString("Column_Cam4"),
            Models.DataType.Cam5 => GetString("Column_Cam5"),
            Models.DataType.Cam6 => GetString("Column_Cam6"),
            _ => dataType.ToString()
        };
    }

    /// <summary>
    /// 데이터 타입에 따른 표시 이름을 가져옵니다.
    /// </summary>
    public static string GetDataTypeDisplayName(Models.DataType dataType)
    {
        return dataType switch
        {
            Models.DataType.Normal => GetString("DataType_Normal"),
            Models.DataType.NIR => GetString("DataType_NIR"),
            Models.DataType.Cam1 => GetString("DataType_Camera1"),
            Models.DataType.Cam2 => GetString("DataType_Camera2"),
            Models.DataType.Cam3 => GetString("DataType_Camera3"),
            Models.DataType.Cam4 => GetString("DataType_Camera4"),
            Models.DataType.Cam5 => GetString("DataType_Camera5"),
            Models.DataType.Cam6 => GetString("DataType_Camera6"),
            _ => dataType.ToString()
        };
    }

    /// <summary>
    /// 카메라 레이블을 가져옵니다.
    /// </summary>
    public static string GetCameraLabel(int cameraNumber)
    {
        return cameraNumber switch
        {
            1 => GetString("Camera_Cam1"),
            2 => GetString("Camera_Cam2"),
            3 => GetString("Camera_Cam3"),
            4 => GetString("Camera_Cam4"),
            5 => GetString("Camera_Cam5"),
            6 => GetString("Camera_Cam6"),
            _ => $"Cam {cameraNumber}"
        };
    }
}



