using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/*
 *Unity��OpenAI��API���g�p���ă`���b�g�{�b�g���������邽�߂̃N���X�B 
 * 
 */


public class ChatGptClient
{
    [SerializeField]
    private TextAsset textFile; // ChatGPT����N�����̃v�����v�g���L�ڂ����e�L�X�g�t�@�C���B

    [Serializable]
    public class ChatGPTMessageModel
    {
        public string role; // ���b�Z�[�W�̑��M�҂̖����isystem�p�����[�U�[���p�����ʂ��邽�߂̕ϐ��j
        public string content; // ���b�Z�[�W�̓��e
    }

    [Serializable]
    public class ChatGPTOptions
    {
        public string model; // �g�p���郂�f���̎w��(gpt-3.5-turbo�g�p)
        public List<ChatGPTMessageModel> messages; // �`���b�g�̃��b�Z�[�W����
    }

    [Serializable]
    public class ChatGPTResponseModel
    {
        public string id; // ���X�|���X��ID
        public string @object; // �I�u�W�F�N�g�̎��
        public int created; // ���X�|���X�̍쐬����
        public Choice[] choices; // ��Ă��ꂽ�I�����@��OpenAI���畡���̕ԓ����e�̌�₪�o�͂����B�����̑I�������i�[����z��
        public Usage usage; // ���N�G�X�g�̎g�p��

        [Serializable]
        public class Choice
        {
            public int index; // �I�����̃C���f�b�N�X�BAPI����̃��X�|���X�Ɋ܂܂����
            public ChatGPTMessageModel message; // �I�����̃��b�Z�[�W���e�BAPI����̃��X�|���X�Ɋ܂܂����
            public string finish_reason; // �I�����̏I�����R�BAPI����̃��X�|���X�Ɋ܂܂����
        }

        [Serializable]
        public class Usage
        {
            public int prompt_tokens; // �v�����v�g�̃g�[�N����
            public int completion_tokens; // �����̃g�[�N����
            public int total_tokens; // �S�̂̃g�[�N����
        }
    }

    private readonly string apiKey = "xxxx"; // OpenAI API�̔F�؃L�[
    private readonly string apiUrl = "https://api.openai.com/v1/chat/completions"; // API��URL
    private readonly List<ChatGPTMessageModel> messageList = new List<ChatGPTMessageModel>(); // ���b�Z�[�W�̃��X�g�BAPI����Ԃ��ė����ԓ����e���i�[����B

    public ChatGptClient()
    {
        // TextAsset�̓��e�𕶎���Ƃ��Ď擾
        UnityEngine.TextAsset textFile = Resources.Load<TextAsset>("Jobtext");
        string textDate = textFile.text;

        // �e�L�X�g�f�[�^���R���\�[���ɏo��
        Debug.Log(textDate);
        messageList.Add(new ChatGPTMessageModel() { role = "system", content = textDate }); // �e�L�X�g���V�X�e�����b�Z�[�W�Ƃ��ă��X�g�ɒǉ�����B
    }

    public async UniTask<string> RequestAsync(string userMessage)
    {
        messageList.Add(new ChatGPTMessageModel { role = "user", content = userMessage }); // ���[�U�[���b�Z�[�W�����X�g�ɒǉ�����B

        Dictionary<string, string> headers = GetAPIRequestHeaders(); // API���N�G�X�g�p�̃w�b�_�[���擾�B�w�b�_�[�ɂ͔F�ؗp�̃g�[�N���Ȃǂ̏�񂪊܂܂��B

        /*
        �ZDictionary
        �@�ˁ@�L�[�ƒl�̃y�A���Ǘ�����B�udictionary["�L�[���"];�v�Œl���o�͂��邱�Ƃ��o����B
        */


ChatGPTOptions options = GetChatGptOptions(); // �`���b�gGPT�̃I�v�V�������擾
        string jsonOptions = JsonUtility.ToJson(options); // �I�v�V������JSON�`���ɕϊ�

        UnityWebRequest request = GetRequest(jsonOptions); // API���N�G�X�g���쐬

        foreach (var header in headers)
        {
            request.SetRequestHeader(header.Key, header.Value); // �e�w�b�_�[�����N�G�X�g�ɒǉ�
        }

        await request.SendWebRequest(); // ���N�G�X�g�𑗐M���A�ҋ@

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(request.error); // API���ŃG���[�����������ꍇ�A�G���[���O���o�͂��o�͂���B
            throw new Exception(); // ��O���X���[�i�A���[�g���o���j
        }
        else
        {
            var responseString = request.downloadHandler.text; // ���X�|���X�𕶎���Ƃ��Ď擾
            var responseObject = JsonUtility.FromJson<ChatGPTResponseModel>(responseString); // json�`���̃��X�|���X��ChatGPTResponseModel�Ƀf�V���A���C�Y
            string replyMessage = responseObject.choices[0].message.content; // �ŏ��̑I�����̃��b�Z�[�W���e���擾

            return replyMessage; // �ԐM���b�Z�[�W��Ԃ�
        }
    }

    private Dictionary<string, string> GetAPIRequestHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            {"Authorization", "Bearer " + apiKey}, // �F�؃w�b�_�[
            {"Content-type", "application/json"}, // �R���e���c�^�C�v�w�b�_�[
            //{"X-Slack-No-Retry", "1"} // Slack���g���C�������w�b�_�[�B�Ȃ��Ă����Ȃ��H
        };

        return headers; // �w�b�_�[��Ԃ�
    }

    private ChatGPTOptions GetChatGptOptions()
    {
        var options = new ChatGPTOptions()
        {
            model = "gpt-3.5-turbo", // �g�p����GPT���f��
            messages = messageList // �g�p���郁�b�Z�[�W���X�g
        };

        return options; // �I�v�V������Ԃ�
    }

    private UnityWebRequest GetRequest(string jsonOptions)
    {
        var request = new UnityWebRequest(apiUrl, "POST") // POST���\�b�h(HTTP�ł̒ʐM�̃v���g�R��)�ł�API���N�G�X�g���쐬
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonOptions)), // ���N�G�X�g�{���̐ݒ�
            downloadHandler = new DownloadHandlerBuffer() // ���X�|���X�̃n���h���[�ݒ�
        };

        return request; // ���N�G�X�g��Ԃ�
    }
}
