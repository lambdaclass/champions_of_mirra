using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.VFX;

public class PoolsHandler : MonoBehaviour
{
    private Dictionary<string, MMSimpleObjectPooler> poolsPoolers;

    HashSet<SkillInfo> poolSkillsInfo;

    public Dictionary<int, PoolSkill> poolsFeedbacks = new Dictionary<int, PoolSkill>();

    public IEnumerator SetUpPoolsSkills()
    {
        yield return new WaitUntil(() => GameServerConnectionManager.Instance.players.Count > 0);

        poolSkillsInfo = GameServerConnectionManager.Instance.players
            .SelectMany(player => player.GetComponents<Skill>())
            .Select(skill => skill.GetSkillInfo())
            .Where(skill => skill.hasSkillPool)
            .GroupBy(skill => skill.name)
            .Select(group => group.First())
            .ToHashSet();

        CreatePoolsPoolers(poolSkillsInfo);
    }

    public void CreatePoolsPoolers(HashSet<SkillInfo> skillsInfo)
    {
        poolsPoolers = new Dictionary<string, MMSimpleObjectPooler>();
        foreach (SkillInfo skillInfo in skillsInfo)
        {
            MMSimpleObjectPooler poolsPooler = Utils.SimpleObjectPooler(
                skillInfo.poolPrefab.name + "_Pooler",
                transform.parent,
                skillInfo.poolPrefab
            );
            poolsPoolers.Add($"{skillInfo.name}_Pooler", poolsPooler);
        }
    }

    public void UpdatePoolsActions()
    {
        List<Entity> poolsStates = GameServerConnectionManager.Instance.gamePools;
        ClearPools(poolsStates);
        SpawnPools(poolsStates);
        HandlePoolsEffects(poolsStates);
    }

    private void ClearPools(List<Entity> poolsStates)
    {
        foreach (int poolId in poolsFeedbacks.Keys.ToList())
        {
            if (!poolsStates.Exists(x => (int)x.Id == poolId))
            {
                poolsFeedbacks[poolId].TurnOff();
                poolsFeedbacks.Remove(poolId);
            }
        }
    }

    private void SpawnPools(List<Entity> poolsStates)
    {
        foreach(Entity poolState in poolsStates)
        {
            if(!poolsFeedbacks.Keys.Contains((int)poolState.Id))
            {
                string poolSkillKey = poolState.Pool.SkillKey;
                ulong skillOwner = poolState.Pool.OwnerId;

                SkillInfo skillInfo = poolSkillsInfo
                    .Where(el => el.poolSkillKey == poolSkillKey)
                    .FirstOrDefault();

                PoolSkill poolFeedback = InstantiatePool(
                    skillInfo,
                    Utils.transformBackendOldPositionToFrontendPosition(poolState.Position),
                    Utils.TransformBackenUnitToClientUnit(poolState.Radius)
                );

                poolsFeedbacks.Add((int)poolState.Id, poolFeedback);
            }
        }
    }

    private void HandlePoolsEffects(List<Entity> poolsStates)
    {
        foreach(Entity poolState in poolsStates)
        {
            PoolSkill poolFeedback = poolsFeedbacks[(int)poolState.Id];
            poolFeedback.HandlePoolEffects(poolState.Pool.Effects);

            poolsFeedbacks[(int)poolState.Id] = poolFeedback;
        }
    }

    public PoolSkill InstantiatePool(SkillInfo skillInfo, Vector3 initialPosition, float radius)
    {
        MMSimpleObjectPooler poolsPooler = poolsPoolers[$"{skillInfo.name}_Pooler"];
        PoolSkill poolSkill = poolsPooler.GetPooledGameObject().GetComponent<PoolSkill>();
        poolSkill.Initialize(skillInfo, initialPosition, radius);

        return poolSkill;
    }
}
