using UnityEngine;
using SOInterfaces;
public sealed class PlayerInstaller : MonoBehaviour
{
    [SerializeField] private CharacterSpec spec;
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
        controller.spec = spec;
    }
}