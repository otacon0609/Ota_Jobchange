using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/*
 *UnityでOpenAIのAPIを使用してチャットボットを実装するためのクラス。 
 * 
 */


public class ChatGptClient
{
    [SerializeField]
    private TextAsset textFile; // ChatGPT初回起動時のプロンプトを記載したテキストファイル。

    [Serializable]
    public class ChatGPTMessageModel
    {
        public string role; // メッセージの送信者の役割（system用かユーザー利用か判別するための変数）
        public string content; // メッセージの内容
    }

    [Serializable]
    public class ChatGPTOptions
    {
        public string model; // 使用するモデルの指定(gpt-3.5-turbo使用)
        public List<ChatGPTMessageModel> messages; // チャットのメッセージ履歴
    }

    [Serializable]
    public class ChatGPTResponseModel
    {
        public string id; // レスポンスのID
        public string @object; // オブジェクトの種類
        public int created; // レスポンスの作成日時
        public Choice[] choices; // 提案された選択肢　※OpenAIから複数の返答内容の候補が出力される。複数の選択肢を格納する配列
        public Usage usage; // リクエストの使用状況

        [Serializable]
        public class Choice
        {
            public int index; // 選択肢のインデックス。APIからのレスポンスに含まれる情報
            public ChatGPTMessageModel message; // 選択肢のメッセージ内容。APIからのレスポンスに含まれる情報
            public string finish_reason; // 選択肢の終了理由。APIからのレスポンスに含まれる情報
        }

        [Serializable]
        public class Usage
        {
            public int prompt_tokens; // プロンプトのトークン数
            public int completion_tokens; // 完了のトークン数
            public int total_tokens; // 全体のトークン数
        }
    }

    private readonly string apiKey = "xxxx"; // OpenAI APIの認証キー
    private readonly string apiUrl = "https://api.openai.com/v1/chat/completions"; // APIのURL
    private readonly List<ChatGPTMessageModel> messageList = new List<ChatGPTMessageModel>(); // メッセージのリスト。APIから返って来た返答内容を格納する。

    public ChatGptClient()
    {
        // TextAssetの内容を文字列として取得
        UnityEngine.TextAsset textFile = Resources.Load<TextAsset>("Jobtext");
        string textDate = textFile.text;

        // テキストデータをコンソールに出力
        Debug.Log(textDate);
        messageList.Add(new ChatGPTMessageModel() { role = "system", content = textDate }); // テキストをシステムメッセージとしてリストに追加する。
    }

    public async UniTask<string> RequestAsync(string userMessage)
    {
        messageList.Add(new ChatGPTMessageModel { role = "user", content = userMessage }); // ユーザーメッセージをリストに追加する。

        Dictionary<string, string> headers = GetAPIRequestHeaders(); // APIリクエスト用のヘッダーを取得。ヘッダーには認証用のトークンなどの情報が含まれる。

        /*
        〇Dictionary
        　⇒　キーと値のペアを管理する。「dictionary["キー情報"];」で値を出力することが出来る。
        */


ChatGPTOptions options = GetChatGptOptions(); // チャットGPTのオプションを取得
        string jsonOptions = JsonUtility.ToJson(options); // オプションをJSON形式に変換

        UnityWebRequest request = GetRequest(jsonOptions); // APIリクエストを作成

        foreach (var header in headers)
        {
            request.SetRequestHeader(header.Key, header.Value); // 各ヘッダーをリクエストに追加
        }

        await request.SendWebRequest(); // リクエストを送信し、待機

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(request.error); // API側でエラーが発生した場合、エラーログを出力を出力する。
            throw new Exception(); // 例外をスロー（アラートを出す）
        }
        else
        {
            var responseString = request.downloadHandler.text; // レスポンスを文字列として取得
            var responseObject = JsonUtility.FromJson<ChatGPTResponseModel>(responseString); // json形式のレスポンスをChatGPTResponseModelにデシリアライズ
            string replyMessage = responseObject.choices[0].message.content; // 最初の選択肢のメッセージ内容を取得

            return replyMessage; // 返信メッセージを返す
        }
    }

    private Dictionary<string, string> GetAPIRequestHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            {"Authorization", "Bearer " + apiKey}, // 認証ヘッダー
            {"Content-type", "application/json"}, // コンテンツタイプヘッダー
            //{"X-Slack-No-Retry", "1"} // Slackリトライ無効化ヘッダー。なくても問題ない？
        };

        return headers; // ヘッダーを返す
    }

    private ChatGPTOptions GetChatGptOptions()
    {
        var options = new ChatGPTOptions()
        {
            model = "gpt-3.5-turbo", // 使用するGPTモデル
            messages = messageList // 使用するメッセージリスト
        };

        return options; // オプションを返す
    }

    private UnityWebRequest GetRequest(string jsonOptions)
    {
        var request = new UnityWebRequest(apiUrl, "POST") // POSTメソッド(HTTPでの通信のプロトコル)でのAPIリクエストを作成
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonOptions)), // リクエスト本文の設定
            downloadHandler = new DownloadHandlerBuffer() // レスポンスのハンドラー設定
        };

        return request; // リクエストを返す
    }
}
