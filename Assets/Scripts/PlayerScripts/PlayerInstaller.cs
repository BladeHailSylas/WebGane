using UnityEngine;

public sealed class PlayerInstaller : MonoBehaviour
{
    [SerializeField] private PlayerCharacterSpec spec;
    [SerializeField] private PlayerAttackController controller;

    private void Awake()
    {
        if (spec == null || controller == null)
        {
            Debug.LogError("[PlayerInstaller] Spec �Ǵ� Controller�� ��� �ֽ��ϴ�.");
            return;
        }

        // ���⼭ �ƹ� �ϵ� ���� �ʰ�,
        // PlayerAttackController�� ������ Awake���� spec�� �е��� �Ӵϴ�.
        controller.Spec = spec;
    }
}