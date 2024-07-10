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

    // AdvEngine�̎Q�Ƃ�ێ�����v���p�e�B
    public AdvEngine Engine { get { return this.engine ?? (this.engine = FindObjectOfType<AdvEngine>() as AdvEngine); } }
    [SerializeField] AdvEngine engine;

    // VoiceBox�N���p
    [SerializeField] VOICEVOX voicevox;
    private int SpeakerId = 1; // ���񂾂���̐�

    //��3�Ɋւ���̕ϐ�
    public string Label;//�ǂ��̃V�[���ɃW�����v���邩�̃��x��
    public string emotion;//���񂾂���̊���
    public bool Tork_flag;//�}�C�N���͎�t�����ǂ���

    // ChatGptClient�̃C���X�^���X
    ChatGptClient chatGptClient;

    // UI�v�f�̎Q��
    [SerializeField] private GameObject Mic; //�}�C�N�A�C�R��
    [SerializeField] private GameObject Window; //���b�Z�[�W�E�B���h�E
    [SerializeField] private GameObject Kizuku; //���񂾂��񂪉������󂯎�������̃A�C�R��

    // �R���[�`���̎��s��Ԃ������t���O
    private bool isCoroutineRunning = false;

    // DictationResult�i�����F���̌��ʁj�̃C�x���g�n���h�����o�^����Ă��邩�������t���O
    private bool isDictationHandlerRegistered = false;

    // �����F���̏�������ChatGptClient�̃C���X�^���X��
    async void Start()
    {
        dictationRecognizer = new DictationRecognizer();

        // DictationResult �̃C�x���g�n���h����o�^����i��x�����j
        if (!isDictationHandlerRegistered)
        {
            dictationRecognizer.DictationResult += DictationResultHandler;
            isDictationHandlerRegistered = true;
        }

        // ChatGptClient �̃C���X�^���X��������
        chatGptClient = new ChatGptClient();
    }

    // �Q�[�����[�v���ł̍X�V����
    async void Update()
    {
        // AdvEngine����̃t���O�擾
        Tork_flag = (bool)Engine.Param.GetParameter("Tork_f");

        // Tork_flag��true�ł��R���[�`�������s���łȂ��ꍇ
        if (Tork_flag == true && !isCoroutineRunning)
        {
            Engine.Param.TrySetParameter("Tork_f", false);
            Mic.SetActive(true);
            Window.SetActive(false);
            Tork_flag = false;
            StartCoroutine(WaitForSecondsCoroutine(1.0f));
        }
    }

    // DictationRecognizer��DictationResult�C�x���g�n���h��
    private void DictationResultHandler(string text, ConfidenceLevel confidence)
    {
        // �����F���̌��ʂ����O�o��
        Debug.Log(text);

        // ���񂾂��񂪉������͂ɋC�t�������̃A�C�R���B
        Kizuku.SetActive(true);


        dictationRecognizer.Stop();

        // ���[�U�[�̔�����ChatGptClient�ɑ��M���ĕԓ�������
        ProcessChatGptResponseAsync(text);
    }

    // ChatGptClient���g�p���Ĕ񓯊���OpenAI�Ƀ��N�G�X�g�𑗐M���A�ԓ�������
    private async void ProcessChatGptResponseAsync(string text)
    {
        try
        {
            //���b�Z�[�W�E�B���h�E��\��
            Window.SetActive(true);

            // ChatGptClient���g�p���ĕԓ����擾
            string replyMessage = await chatGptClient.RequestAsync(text);
            Debug.Log("Reply = " + replyMessage);

            // �ԓ����犴��𔻕ʂ��A����ɉ��������x����ݒ�
            emotion = replyMessage.Substring(0, 3);
            replyMessage = replyMessage.Remove(0, 3);
            voicevox.PlayOneShot(SpeakerId, replyMessage);
            SetLabelFromEmotion();

            // AdvEngine�ɕԓ���ݒ肵�A�V�i���I�W�����v
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

    // �w��b���ҋ@����R���[�`��
    private IEnumerator WaitForSecondsCoroutine(float seconds)
    {
        isCoroutineRunning = true;// �R���[�`���J�n���Ƀt���O���Z�b�g
        yield return new WaitForSeconds(seconds);
        dictationRecognizer.Start();
        dictationRecognizer.Stop();
        isCoroutineRunning = false; // �R���[�`���I�����Ƀt���O�����Z�b�g
    }

    // �j�����ꂽ�Ƃ���DictationRecognizer�����
    private void OnDestroy()
    {
        dictationRecognizer.Dispose();
    }

    // ���X�|���X���犴��𒊏o���A����Ɋ�Â��ĉ�b�̃��x����ݒ肷�郁�\�b�h
    /* 
     * ���X�|���X���犴��𒊏o���܂��B�����OpenAI�̃v�����v�g�Ɋ�Â��āA
     * ���t�̍ŏ���< >��t���āA<>���̋󗓂Ɍ��݂̊����\�������������Ă��܂��B
     * �ȉ��̂悤�ɑΉ��t�����܂��F
     *   �������ꍇ   ��   <�P>
     *   �{���Ă���ꍇ ��   <�Q>
     *   �߂����ꍇ   ��   <�R>
     *   �y�����ꍇ   ��   <�S>
     *   ���ʂ̏ꍇ   ��   <�T>
     */
    private void SetLabelFromEmotion()
    {
        switch (emotion)
        {
            case "<�P>":
                Label = "��b_��";
                break;
            case "<�Q>":
                Label = "��b_�{";
                break;
            case "<�R>":
                Label = "��b_��";
                break;
            case "<�S>":
                Label = "��b_�y";
                break;
            case "<�T>":
                Label = "��b_����";
                break;
            default:
                Label = "��b_����";
                break;
        }
    }
}
