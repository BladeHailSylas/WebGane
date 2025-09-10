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
            Debug.LogError("[PlayerInstaller] Spec 또는 Controller가 비어 있습니다.");
            return;
        }

        // 여기서 아무 일도 하지 않고,
        // PlayerAttackController가 스스로 Awake에서 spec을 읽도록 둡니다.
        controller.spec = spec;
    }
}