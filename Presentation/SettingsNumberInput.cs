using System.Globalization;

namespace AutoExtracter;

/// <summary>
/// 増減ボタンを持たず数値を直接入力する
/// </summary>
internal sealed class SettingsNumberInput : TextBox
{
    /// <summary>
    /// 入力できる最小値
    /// </summary>
    internal decimal Minimum
    {
        get;
        set;
    }

    /// <summary>
    /// 入力できる最大値
    /// </summary>
    internal decimal Maximum
    {
        get;
        set;
    } = decimal.MaxValue;

    /// <summary>
    /// 表示と保存に使用する小数点以下の桁数
    /// </summary>
    internal int DecimalPlaces
    {
        get;
        set;
    } = 2;

    /// <summary>
    /// 桁区切りを表示するか
    /// </summary>
    internal bool ThousandsSeparator
    {
        get;
        set;
    }

    /// <summary>
    /// 入力された数値を検証し、設定値を共通の書式で表示する
    /// </summary>
    internal decimal Value
    {
        get
        {
            if (!decimal.TryParse(Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal value) ||
                value < Minimum || value > Maximum)
            {
                throw new ArgumentException(Messages.Get(MessageId.E103));
            }

            return decimal.Round(value, DecimalPlaces);
        }

        set
        {
            Text = value.ToString((ThousandsSeparator ? "N" : "F") + DecimalPlaces, CultureInfo.CurrentCulture);
        }
    }
}
