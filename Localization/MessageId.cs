namespace AutoExtracter;

/// <summary>
/// メッセージの種別と番号を定義する
/// </summary>
internal enum MessageId
{
    /// <summary>
    /// 監視開始: 対象
    /// </summary>
    I001,
    /// <summary>
    /// 監視終了
    /// </summary>
    I002,
    /// <summary>
    /// ファイルの受け入れと処理順序を通知する
    /// </summary>
    I003,
    /// <summary>
    /// 解凍開始: 対象
    /// </summary>
    I004,
    /// <summary>
    /// 解凍完了: 対象 → 保存先
    /// </summary>
    I005,
    /// <summary>
    /// パスワードが必要なジョブの優先度を下げる
    /// </summary>
    I006,
    /// <summary>
    /// パスワードが必要なため退避した対象と保存先
    /// </summary>
    W001,
    /// <summary>
    /// 起動または実行の失敗
    /// </summary>
    E001,
    /// <summary>
    /// 監視スキャン失敗
    /// </summary>
    E002,
    /// <summary>
    /// 受け入れ失敗: 対象
    /// </summary>
    E003,
    /// <summary>
    /// 処理失敗一時フォルダのデータを保持して再試行する
    /// </summary>
    E004,
    /// <summary>
    /// 解凍失敗: 対象 → 保存先
    /// </summary>
    E005,
    /// <summary>
    /// ログ書き込み失敗: 対象
    /// </summary>
    E006,
    /// <summary>
    /// フォルダの指定不足
    /// </summary>
    E101,
    /// <summary>
    /// 各フォルダの重複や親子関係を検出する
    /// </summary>
    E102,
    /// <summary>
    /// 待機時間と解凍上限の正値を検証する
    /// </summary>
    E103,
    /// <summary>
    /// 使い方: AutoExtracter.exe [appsettings.json のパス]
    /// </summary>
    E104,
    /// <summary>
    /// 設定が空であることを示す
    /// </summary>
    E105,
    /// <summary>
    /// 分割アーカイブには対応していません
    /// </summary>
    E106,
    /// <summary>
    /// エントリ数の上限を超えました
    /// </summary>
    E107,
    /// <summary>
    /// リンクを含むアーカイブには対応していません
    /// </summary>
    E108,
    /// <summary>
    /// ファイル名が空であることを示す
    /// </summary>
    E109,
    /// <summary>
    /// アーカイブに絶対パスが含まれることを示す
    /// </summary>
    E110,
    /// <summary>
    /// アーカイブのパスが解凍先の外を指すことを示す
    /// </summary>
    E111,
    /// <summary>
    /// 解凍サイズの上限を超えました
    /// </summary>
    E112,
    /// <summary>
    /// パスワードが必要であることを示す
    /// </summary>
    E113,
    /// <summary>
    /// ジョブのファイル名が不正であることを示す
    /// </summary>
    E114,
    /// <summary>
    /// 配布用アプリのビルド失敗を通知する
    /// </summary>
    E202,

    /// <summary>
    /// 待機中
    /// </summary>
    I301,
    /// <summary>
    /// 解凍中
    /// </summary>
    I302,
    /// <summary>
    /// 完了フォルダへ転送中
    /// </summary>
    I303,
    /// <summary>
    /// エラー発生・再試行待ち
    /// </summary>
    I304,
    /// <summary>
    /// 終了処理中
    /// </summary>
    I305,
    /// <summary>
    /// 終了
    /// </summary>
    I307,
    /// <summary>
    /// AutoExtracter 解凍状況
    /// </summary>
    I310,
    /// <summary>
    /// 計算中／不明
    /// </summary>
    I311,
    /// <summary>
    /// 処理状況の表示形式
    /// </summary>
    I313,
    /// <summary>
    /// 圧縮ファイルの追加を待機中
    /// </summary>
    I322,
    /// <summary>
    /// フォルダを開けません: {0} {1}
    /// </summary>
    E115,
    /// <summary>
    /// GIFの読み込み制限を通知する
    /// </summary>
    E116,
    /// <summary>
    /// 待機中のローディング表示
    /// </summary>
    I326,
    /// <summary>
    /// 待機GIFの読み込み失敗を通知する
    /// </summary>
    W003,
    /// <summary>
    /// 圧縮 {0} {1} / {2}
    /// </summary>
    I328,
    /// <summary>
    /// 解凍中GIFを読み込めないため同梱画像を使用する: {0}
    /// </summary>
    W004,
    /// <summary>
    /// 監視中：{0}
    /// </summary>
    I330,
    /// <summary>
    /// 設定
    /// </summary>
    I334,
    /// <summary>
    /// 参照
    /// </summary>
    I336,
    /// <summary>
    /// 本日のログファイルはまだありません
    /// </summary>
    I342,
    /// <summary>
    /// 本日分のログファイルを開く
    /// </summary>
    I343,
    /// <summary>
    /// 対象拡張子の未選択や未対応形式を検出する
    /// </summary>
    E117,
    /// <summary>
    /// 設定を保存できません: {0}
    /// </summary>
    E118,
    /// <summary>
    /// 外観
    /// </summary>
    I344,
    /// <summary>
    /// ライト
    /// </summary>
    I345,
    /// <summary>
    /// ダーク
    /// </summary>
    I346,
    /// <summary>
    /// システムに合わせる
    /// </summary>
    I347,
    /// <summary>
    /// 未対応のテーマ指定を検出する
    /// </summary>
    E119,
    /// <summary>
    /// 問題のあるファイル（{0}件）
    /// </summary>
    I348,
    /// <summary>
    /// 閉じる記号
    /// </summary>
    I352,
    /// <summary>
    /// 退避フォルダの件数を確認できないため前回値を保持する: {0}
    /// </summary>
    W005,
    /// <summary>
    /// 処理設定
    /// </summary>
    I353,
    /// <summary>
    /// その他
    /// </summary>
    I355,
    /// <summary>
    /// 監視対象フォルダ
    /// </summary>
    I356,
    /// <summary>
    /// 移動先フォルダ
    /// </summary>
    I357,
    /// <summary>
    /// 対象拡張子
    /// </summary>
    I358,
    /// <summary>
    /// 上限容量（解凍後・バイト）
    /// </summary>
    I359,
    /// <summary>
    /// 適用
    /// </summary>
    I366,
    /// <summary>
    /// 設定を閉じる
    /// </summary>
    I371,
    /// <summary>
    /// 処理設定を保存したためアプリを再起動する
    /// </summary>
    I372,
    /// <summary>
    /// GIF画像|*.gif
    /// </summary>
    I373,
    /// <summary>
    /// 選択中のセクション名
    /// </summary>
    I376,
    /// <summary>
    /// 未選択のセクション名
    /// </summary>
    I377,
    /// <summary>
    /// 外観設定
    /// </summary>
    I378,
    /// <summary>
    /// アイコン設定
    /// </summary>
    I381,
    /// <summary>
    /// 待機中アイコン
    /// </summary>
    I382,
    /// <summary>
    /// 処理中アイコン
    /// </summary>
    I383,
    /// <summary>
    /// 初期設定
    /// </summary>
    I384,
    /// <summary>
    /// パスワード付きファイルの扱い
    /// </summary>
    I385,
    /// <summary>
    /// パスワード入力待ち
    /// </summary>
    I386,
    /// <summary>
    /// パスワードを求める通知
    /// </summary>
    I387,
    /// <summary>
    /// 対話アイコン
    /// </summary>
    I390,
    /// <summary>
    /// パスワード
    /// </summary>
    I391,
    /// <summary>
    /// 解凍
    /// </summary>
    I392,
    /// <summary>
    /// 退避
    /// </summary>
    I393,
    /// <summary>
    /// 画面なし実行では退避する
    /// </summary>
    W006,
    /// <summary>
    /// パスワードの再入力を求める
    /// </summary>
    W007,
    /// <summary>
    /// 対話GIFの読み込み失敗
    /// </summary>
    W008,
    /// <summary>
    /// アプリがすでに起動している
    /// </summary>
    W009,
    /// <summary>
    /// アニメーション設定
    /// </summary>
    I394,
    /// <summary>
    /// 圧縮ファイル容量の上限超過
    /// </summary>
    E120,
    /// <summary>
    /// デスクトップのショートカット作成
    /// </summary>
    I395,
    /// <summary>
    /// スタートアップの設定
    /// </summary>
    I396,
    /// <summary>
    /// インストールフォルダを開く
    /// </summary>
    I397,
    /// <summary>
    /// ショートカットの作成完了
    /// </summary>
    I398,
    /// <summary>
    /// スタートアップの設定完了
    /// </summary>
    I399,
    /// <summary>
    /// ショートカット操作の失敗
    /// </summary>
    E121,
    /// <summary>
    /// メッセージの承認
    /// </summary>
    I400,
    /// <summary>
    /// メッセージの拒否
    /// </summary>
    I401,
    /// <summary>
    /// 再起動の確認
    /// </summary>
    I402,
    /// <summary>
    /// 次回の確認省略
    /// </summary>
    I403,
    /// <summary>
    /// 元の圧縮ファイルを保持する設定
    /// </summary>
    I404,
    /// <summary>
    /// 圧縮ファイルの退避先
    /// </summary>
    I405,
    /// <summary>
    /// Visual Studio用のインストールファイル準備完了
    /// </summary>
    I204,
    /// <summary>
    /// MSIの作成完了
    /// </summary>
    I205,
    /// <summary>
    /// MSIのビルド失敗
    /// </summary>
    E214,
    /// <summary>
    /// インストーラーの収録ファイルが未作成
    /// </summary>
    E215,
    /// <summary>
    /// セットアップ用メッセージ I500
    /// </summary>
    I500,

    /// <summary>
    /// セットアップ用メッセージ I501
    /// </summary>
    I501,

    /// <summary>
    /// セットアップ用メッセージ I502
    /// </summary>
    I502,

    /// <summary>
    /// セットアップ用メッセージ I503
    /// </summary>
    I503,

    /// <summary>
    /// セットアップ用メッセージ I504
    /// </summary>
    I504,

    /// <summary>
    /// セットアップ用メッセージ I507
    /// </summary>
    I507,

    /// <summary>
    /// セットアップ用メッセージ I508
    /// </summary>
    I508,

    /// <summary>
    /// セットアップ用メッセージ I509
    /// </summary>
    I509,

    /// <summary>
    /// セットアップ用メッセージ I510
    /// </summary>
    I510,

    /// <summary>
    /// セットアップ用メッセージ I511
    /// </summary>
    I511,

    /// <summary>
    /// セットアップ用メッセージ I512
    /// </summary>
    I512,

    /// <summary>
    /// セットアップ用メッセージ I516
    /// </summary>
    I516,

    /// <summary>
    /// セットアップ用メッセージ I517
    /// </summary>
    I517,

    /// <summary>
    /// セットアップ用メッセージ I518
    /// </summary>
    I518,

    /// <summary>
    /// セットアップ用メッセージ I519
    /// </summary>
    I519,

    /// <summary>
    /// セットアップ用メッセージ I520
    /// </summary>
    I520,

    /// <summary>
    /// セットアップ用メッセージ I521
    /// </summary>
    I521,

    /// <summary>
    /// セットアップ用メッセージ I522
    /// </summary>
    I522,

    /// <summary>
    /// セットアップ用メッセージ I523
    /// </summary>
    I523,

    /// <summary>
    /// セットアップ用メッセージ I524
    /// </summary>
    I524,

    /// <summary>
    /// セットアップ用メッセージ I525
    /// </summary>
    I525,

    /// <summary>
    /// セットアップ用メッセージ I526
    /// </summary>
    I526,

    /// <summary>
    /// セットアップ用メッセージ I527
    /// </summary>
    I527,

    /// <summary>
    /// セットアップ用メッセージ I528
    /// </summary>
    I528,

    /// <summary>
    /// セットアップ用メッセージ I529
    /// </summary>
    I529,

    /// <summary>
    /// セットアップ用メッセージ I530
    /// </summary>
    I530,

    /// <summary>
    /// セットアップ用メッセージ I531
    /// </summary>
    I531,

    /// <summary>
    /// セットアップ用メッセージ I532
    /// </summary>
    I532,

    /// <summary>
    /// セットアップ用メッセージ I533
    /// </summary>
    I533,

    /// <summary>
    /// セットアップ用メッセージ I534
    /// </summary>
    I534,

    /// <summary>
    /// セットアップ用メッセージ I535
    /// </summary>
    I535,

    /// <summary>
    /// セットアップ用メッセージ I536
    /// </summary>
    I536,

    /// <summary>
    /// セットアップ用メッセージ I537
    /// </summary>
    I537,

    /// <summary>
    /// セットアップ用メッセージ I538
    /// </summary>
    I538,

    /// <summary>
    /// セットアップ用メッセージ E501
    /// </summary>
    E501,

    /// <summary>
    /// セットアップ用メッセージ E502
    /// </summary>
    E502,

    /// <summary>
    /// セットアップ用メッセージ E503
    /// </summary>
    E503,

    /// <summary>
    /// セットアップ用メッセージ E504
    /// </summary>
    E504,

    /// <summary>
    /// セットアップ用メッセージ E505
    /// </summary>
    E505,

    /// <summary>
    /// セットアップ用メッセージ E506
    /// </summary>
    E506,

    /// <summary>
    /// セットアップ用メッセージ E507
    /// </summary>
    E507,
}
