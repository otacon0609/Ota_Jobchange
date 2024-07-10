using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.UI;
using Utage;
using VoicevoxBridge;
using UnityEditor.VersionControl;

public class DictationProcessor : MonoBehaviour
{
    DictationRecognizer dictationRecognizer;

    // AdvEngineの参照を保持するプロパティ
    public AdvEngine Engine { get { return this.engine ?? (this.engine = FindObjectOfType<AdvEngine>() as AdvEngine); } }
    [SerializeField] AdvEngine engine;

    // VoiceBox起動用
    [SerializeField] VOICEVOX voicevox;
    private int SpeakerId = 1; // ずんだもんの声

    //宴3に関するの変数
    public string Label;//どこのシーンにジャンプするかのラベル
    public string emotion;//ずんだもんの感情
    public bool Tork_flag;//マイク入力受付中かどうか

    // ChatGptClientのインスタンス
    ChatGptClient chatGptClient;

    // UI要素の参照
    [SerializeField] private GameObject Mic; //マイクアイコン
    [SerializeField] private GameObject Window; //メッセージウィンドウ
    [SerializeField] private GameObject Kizuku; //ずんだもんが音声を受け取った時のアイコン

    // コルーチンの実行状態を示すフラグ
    private bool isCoroutineRunning = false;

    // DictationResult（音声認識の結果）のイベントハンドラが登録されているかを示すフラグ
    private bool isDictationHandlerRegistered = false;

    // 音声認識の初期化とChatGptClientのインスタンス化
    async void Start()
    {
        dictationRecognizer = new DictationRecognizer();

        // DictationResult のイベントハンドラを登録する（一度だけ）
        if (!isDictationHandlerRegistered)
        {
            dictationRecognizer.DictationResult += DictationResultHandler;
            isDictationHandlerRegistered = true;
        }

        // ChatGptClient のインスタンスを初期化
        chatGptClient = new ChatGptClient();
    }

    // ゲームループ内での更新処理
    async void Update()
    {
        // AdvEngineからのフラグ取得
        Tork_flag = (bool)Engine.Param.GetParameter("Tork_f");

        // Tork_flagがtrueでかつコルーチンが実行中でない場合
        if (Tork_flag == true && !isCoroutineRunning)
        {
            Engine.Param.TrySetParameter("Tork_f", false);
            Mic.SetActive(true);
            Window.SetActive(false);
            Tork_flag = false;
            StartCoroutine(WaitForSecondsCoroutine(1.0f));
        }
    }

    // DictationRecognizerのDictationResultイベントハンドラ
    private void DictationResultHandler(string text, ConfidenceLevel confidence)
    {
        // 音声認識の結果をログ出力
        Debug.Log(text);

        // ずんだもんが音声入力に気付いた時のアイコン。
        Kizuku.SetActive(true);


        dictationRecognizer.Stop();

        // ユーザーの発言をChatGptClientに送信して返答を処理
        ProcessChatGptResponseAsync(text);
    }

    // ChatGptClientを使用して非同期でOpenAIにリクエストを送信し、返答を処理
    private async void ProcessChatGptResponseAsync(string text)
    {
        try
        {
            //メッセージウィンドウを表示
            Window.SetActive(true);

            // ChatGptClientを使用して返答を取得
            string replyMessage = await chatGptClient.RequestAsync(text);
            Debug.Log("Reply = " + replyMessage);

            // 返答から感情を判別し、それに応じたラベルを設定
            emotion = replyMessage.Substring(0, 3);
            replyMessage = replyMessage.Remove(0, 3);
            voicevox.PlayOneShot(SpeakerId, replyMessage);
            SetLabelFromEmotion();

            // AdvEngineに返答を設定し、シナリオジャンプ
            Kizuku.SetActive(false);
            Engine.Param.TrySetParameter("Tork_res", replyMessage);
            Mic.SetActive(false);
            Engine.JumpScenario(Label);

        }
        catch (System.Exception ex)
        {
            Debug.LogError("ChatGptClient RequestAsync error: " + ex.Message);
        }
    }

    // 指定秒数待機するコルーチン
    private IEnumerator WaitForSecondsCoroutine(float seconds)
    {
        isCoroutineRunning = true;// コルーチン開始時にフラグをセット
        yield return new WaitForSeconds(seconds);
        dictationRecognizer.Start();
        dictationRecognizer.Stop();
        isCoroutineRunning = false; // コルーチン終了時にフラグをリセット
    }

    // 破棄されたときにDictationRecognizerを解放
    private void OnDestroy()
    {
        dictationRecognizer.Dispose();
    }

    // レスポンスから感情を抽出し、それに基づいて会話のラベルを設定するメソッド
    /* 
     * レスポンスから感情を抽出します。感情はOpenAIのプロンプトに基づいて、
     * 言葉の最初に< >を付けて、<>内の空欄に現在の感情を表す数字が入っています。
     * 以下のように対応付けられます：
     *   嬉しい場合   ⇒   <１>
     *   怒っている場合 ⇒   <２>
     *   悲しい場合   ⇒   <３>
     *   楽しい場合   ⇒   <４>
     *   普通の場合   ⇒   <５>
     */
    private void SetLabelFromEmotion()
    {
        switch (emotion)
        {
            case "<１>":
                Label = "会話_喜";
                break;
            case "<２>":
                Label = "会話_怒";
                break;
            case "<３>":
                Label = "会話_哀";
                break;
            case "<４>":
                Label = "会話_楽";
                break;
            case "<５>":
                Label = "会話_普通";
                break;
            default:
                Label = "会話_普通";
                break;
        }
    }
}
