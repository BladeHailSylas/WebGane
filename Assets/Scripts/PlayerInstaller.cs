using UnityEngine;
using CharacterSOInterfaces;
public sealed class PlayerInstaller : MonoBehaviour
{
    [SerializeField] private PlayerCharacterSpec spec;

    private void Awake()
    {
        if (spec == null)
        {
            Debug.LogError("[PlayerInstaller] Spec이 비어 있습니다.");
            return;
        }

        // Installer 자신에 IPlayerCharacter 컴포넌트가 있으면 초기화
        var pc = GetComponent<IPlayable>();
        /*if (pc == null)
        {
            // 없다면 직접 붙여도 됨
            pc = gameObject.GetComponent<IPlayable>();
        }*/

        pc.InitializeFromSpec(spec);
    }
}