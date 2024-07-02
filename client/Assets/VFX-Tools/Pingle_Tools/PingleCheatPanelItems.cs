using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.TopDownEngine;
using UnityEngine;

public class PingleCheatPanelItems : MonoBehaviour
{
    public GameObject golden_clock_vfx = null;
    public GameObject mirras_blessing_vfx = null;
    public GameObject magic_boots_vfx = null;
    public GameObject giant_fruit_vfx = null;
    public Transform compiler_root = null;

    public AnimationCurve scale_curve = null;
    public float scale_duration = 0.3f;

    public Shader new_character_shader = null;

    public List<Character> character_instances = null;
    public Character muflus_instances = null;
    public float muflus_vfx_scale = 2.0f;
    private List<GameObject> pool = new List<GameObject>();
    private const float ARENA_SIZE = 40.0f;
    private HashSet<Material> mats = new HashSet<Material>();
    private Material[] mats_copy = null;

    public void init()
    {
        //character_instances = FindObjectsOfType<Character>().ToList();
        clearPool();

        foreach( Renderer render in FindObjectsOfType<Renderer>() )
        {
          if ( render.sharedMaterial == null )
            continue;

          if ( render.sharedMaterial.shader == null )
            continue;

          if ( render.sharedMaterial.shader != new_character_shader )
            continue;

          mats.Add( render.sharedMaterial );
        }
        mats_copy = mats.ToArray();

        resetAnims();
    }

    private void resetAnims()
    {
        foreach(Material mat in mats )
            mat.SetFloat("_FresnelEffectAmount", 0);

        foreach(Character character in character_instances)
        {
            character.CharacterAnimator.ResetTrigger("Skill1");
            character.CharacterAnimator.ResetTrigger("Skill2");
            character.CharacterAnimator.ResetTrigger("Skill3");
            character.CharacterAnimator.SetTrigger("Walking");
            character.CharacterAnimator.ResetTrigger("Walking");
        }
    }

    private void clearPool()
    {
        foreach(GameObject go in pool)
            Destroy(go);

        pool.Clear();
    }

    private void OnGUI()
    {
        if ( GUI.Button(new Rect( 100, 100, 80, 80 ), "init") )
            init();

        if ( GUI.Button(new Rect( 200, 100, 80, 80 ), "golden_clock") )
        {
            activateItemVFX(golden_clock_vfx);
        }

        if ( GUI.Button(new Rect( 300, 100, 80, 80 ), "mirras_blessing") )
        {
            activateItemVFX(mirras_blessing_vfx);
        }

        if ( GUI.Button(new Rect( 400, 100, 80, 80 ), "magic_boots") )
        {
            activateItemVFX(magic_boots_vfx);
        }

        if ( GUI.Button(new Rect( 500, 100, 80, 80 ), "giant_fruit") )
        {
            activateItemVFX(giant_fruit_vfx);
            scaleCharacters();
        }
    }

    private void activateItemVFX(GameObject vfx)
    {
        init();

        if ( vfx == null )
          return;

        GameObject cached_vfx = null;
        foreach(Character character in character_instances)
        {
            cached_vfx = Instantiate(vfx, character.transform);
            pool.Add( cached_vfx );
        }
        cached_vfx = Instantiate(vfx, muflus_instances.transform);
        cached_vfx.transform.localScale = new Vector3(muflus_vfx_scale, muflus_vfx_scale, muflus_vfx_scale);
        pool.Add( cached_vfx );
    }

    private void scaleCharacters()
    {
      StartCoroutine(impl());
      IEnumerator impl()
      {
        yield return null;

        float caached_duration = 0;
        Vector3 cached_vector = Vector3.zero;
        while(caached_duration < scale_duration)
        {
          cached_vector.x = scale_curve.Evaluate(caached_duration / scale_duration);
          cached_vector.y = cached_vector.x;
          cached_vector.z = cached_vector.x;

          foreach(Character character in character_instances)
            character.transform.localScale = cached_vector;

          muflus_instances.transform.localScale = cached_vector;
          yield return null;
          caached_duration += Time.deltaTime;
        }

        yield return new WaitForSeconds( 5 - scale_duration - scale_duration );

        caached_duration = scale_duration;
        while(caached_duration > 0)
        {
          cached_vector.x = scale_curve.Evaluate(caached_duration / scale_duration);
          cached_vector.y = cached_vector.x;
          cached_vector.z = cached_vector.x;

          foreach(Character character in character_instances)
            character.transform.localScale = cached_vector;

          muflus_instances.transform.localScale = cached_vector;
          yield return null;
          caached_duration -= Time.deltaTime;
        }
      }
    }

}