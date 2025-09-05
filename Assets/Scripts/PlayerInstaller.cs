using UnityEngine;
using CharacterSOInterfaces;
public sealed class PlayerInstaller : MonoBehaviour
{
    [SerializeField] private PlayerCharacterSpec spec;

    private void Awake()
    {
        if (spec == null)
        {
            Debug.LogError("[PlayerInstaller] Spec�� ��� �ֽ��ϴ�.");
            return;
        }

        // Installer �ڽſ� IPlayerCharacter ������Ʈ�� ������ �ʱ�ȭ
        var pc = GetComponent<IPlayable>();
        /*if (pc == null)
        {
            // ���ٸ� ���� �ٿ��� ��
            pc = gameObject.GetComponent<IPlayable>();
        }*/

        pc.InitializeFromSpec(spec);
    }
}