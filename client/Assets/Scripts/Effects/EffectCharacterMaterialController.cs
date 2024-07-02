using System.Collections;
using UnityEngine;

public class EffectCharacterMaterialController : MonoBehaviour
{
    [SerializeField] private Material character_material = null;
    [SerializeField] private MaterialSettingsKey material_settings_key = null;
    [SerializeField] private float duration = 1.0f;
    [SerializeField] private EffectMaterialControllerType controller_type = EffectMaterialControllerType.MATERIAL;

    private CharacterMaterialManager character_material_manager = null;

    private void Start()
    {
        // print(character_material_manager);
        /// FOR TEST POPOUSES ONLY, DO NOT SHIP
        character_material_manager = GetComponentInParent<CharacterMaterialManager>();
        ///
        switch(controller_type)
        {
            case EffectMaterialControllerType.MATERIAL    : StartCoroutine(switchMaterialForTime()); break;
            case EffectMaterialControllerType.PROPERTIES  : StartCoroutine(applyPropertiesForTime()); break;
        }
    }

    public void Setup(CharacterMaterialManager materialManager){
        character_material_manager = materialManager;
    }

    private void OnDestroy()
    {
        deinit();
    }

    public void init()
    {
        character_material_manager?.setMaterial(character_material);
    }

    public void deinit()
    {
        character_material_manager?.resetMaterial();
    }

    private IEnumerator switchMaterialForTime()
    {
        init();
        yield return new WaitForSeconds(duration);
        deinit();
    }

    private IEnumerator applyPropertiesForTime()
    {
        yield return character_material_manager?.applyEffectByKey(material_settings_key);
        yield return new WaitForSeconds(duration);
        yield return character_material_manager?.deapplyEffectByKey(material_settings_key);
    }
}

public enum EffectMaterialControllerType
{
  NONE = 0,
  MATERIAL = 1,
  PROPERTIES = 2
}