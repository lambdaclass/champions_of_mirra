using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using System.Linq;

public class ProjectileHandler : MonoBehaviour
{
    public List<MMSimpleObjectPooler> objectPoolerList;

    public void CreateProjectilesPoolers(HashSet<SkillInfo> skillInfoSet)
    {
        objectPoolerList = new List<MMSimpleObjectPooler>();
        foreach (SkillInfo skillInfo in skillInfoSet)
        {
            GameObject projectileFromSkill = skillInfo.projectilePrefab;
            MMSimpleObjectPooler objectPooler = Utils.SimpleObjectPooler(
                projectileFromSkill.name + "_Pooler",
                transform.parent,
                projectileFromSkill
            );
            objectPoolerList.Add(objectPooler);
        }
    }

    public GameObject InstanceProjectile(GameObject skillProjectile, float direction, Vector3 initialPosition)
    {
        MMSimpleObjectPooler projectileFromPooler = objectPoolerList.Find(
            objectPooler => objectPooler.name.Contains(skillProjectile.name)
        );
        GameObject pooledGameObject = projectileFromPooler.GetPooledGameObject();
        pooledGameObject.transform.position = initialPosition;
        // Trail
        SkillProjectile pooledSkillProjectile = pooledGameObject.GetComponent<SkillProjectile>();
        var trailobj = pooledSkillProjectile.trail;
        if(pooledSkillProjectile.isTrailEnabled){
            var trailInstance = Instantiate(trailobj, pooledGameObject.transform);
            trailInstance.name = pooledGameObject.name + " Trail";
            pooledSkillProjectile.isTrailEnabled = false;
        }
        pooledGameObject.transform.rotation = Quaternion.Euler(0, direction, 0);
        pooledGameObject.SetActive(true);

        return pooledGameObject;
    }
}
