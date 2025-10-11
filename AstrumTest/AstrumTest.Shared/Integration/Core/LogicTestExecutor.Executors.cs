using System;
using Astrum.LogicCore;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Managers;
using Astrum.LogicCore.Factories;
using Astrum.LogicCore.Physics;
using TrueSync;

namespace AstrumTest.Shared.Integration.Core
{
    /// <summary>
    /// LogicTestExecutor - 输入指令和查询指令执行器
    /// </summary>
    public partial class LogicTestExecutor
    {
        /// <summary>
        /// 执行输入指令（包括创建实体、玩家操作、AI指令等）
        /// </summary>
        private void ExecuteInput(InputCommand input)
        {
            switch (input.Type)
            {
                case "CreateEntity":
                    // 创建新实体
                    if (!_entityTemplates.ContainsKey(input.TemplateId))
                    {
                        throw new Exception($"实体模板不存在: {input.TemplateId}");
                    }
                    
                    var template = _entityTemplates[input.TemplateId];
                    var entity = EntityFactory.Instance.CreateEntity(template.RoleId, World);
                    
                    // 设置位置
                    entity.GetComponent<TransComponent>().Position = input.Position.ToTSVector();
                    
                    // 设置自定义血量
                    if (template.CustomHealth.HasValue)
                    {
                        var health = entity.GetComponent<HealthComponent>();
                        health.MaxHealth = template.CustomHealth.Value;
                        health.CurrentHealth = template.CustomHealth.Value;
                    }
                    
                    // 添加到 Room
                    Room.Players.Add(entity.UniqueId);
                    
                    // 注册到物理世界
                    HitManager.Instance.RegisterEntity(entity);
                    
                    // 保存到映射表
                    _entities[input.EntityId] = entity;
                    
                    _output.WriteLine($"      创建实体: {input.EntityId} (模板={input.TemplateId}, Pos={input.Position.ToTSVector()})");
                    break;
                    
                case "DestroyEntity":
                    if (_entities.ContainsKey(input.EntityId))
                    {
                        var entityToDestroy = _entities[input.EntityId];
                        Room.Players.Remove(entityToDestroy.UniqueId);
                        HitManager.Instance.UnregisterEntity(entityToDestroy);
                        _entities.Remove(input.EntityId);
                        _output.WriteLine($"      销毁实体: {input.EntityId}");
                    }
                    break;
                    
                case "CastSkill":
                    if (!_entities.ContainsKey(input.EntityId))
                    {
                        throw new Exception($"实体不存在: {input.EntityId}");
                    }
                    
                    var caster = _entities[input.EntityId];
                    var actionComp = caster.GetComponent<ActionComponent>();
                    actionComp.CurrentAction = SkillConfigManager.Instance
                        .CreateSkillActionInstance(input.SkillId.Value, 1);
                    actionComp.CurrentFrame = 0;
                    _output.WriteLine($"      {input.EntityId} 施放技能 {input.SkillId} → {input.TargetId}");
                    break;
                    
                case "Move":
                    if (!_entities.ContainsKey(input.EntityId))
                    {
                        throw new Exception($"实体不存在: {input.EntityId}");
                    }
                    
                    var mover = _entities[input.EntityId];
                    mover.GetComponent<TransComponent>().Position = input.TargetPosition.ToTSVector();
                    _output.WriteLine($"      {input.EntityId} 移动到 {input.TargetPosition.ToTSVector()}");
                    break;
                    
                case "Teleport":
                    if (!_entities.ContainsKey(input.EntityId))
                    {
                        throw new Exception($"实体不存在: {input.EntityId}");
                    }
                    
                    var teleporter = _entities[input.EntityId];
                    teleporter.GetComponent<TransComponent>().Position = input.TargetPosition.ToTSVector();
                    _output.WriteLine($"      {input.EntityId} 传送到 {input.TargetPosition.ToTSVector()}");
                    break;
                    
                case "Wait":
                    _output.WriteLine($"      等待");
                    break;
                    
                default:
                    throw new NotImplementedException($"输入类型未实现: {input.Type}");
            }
        }
        
        /// <summary>
        /// 执行查询指令（查询实体状态）
        /// </summary>
        private object ExecuteQuery(QueryCommand query)
        {
            if (!_entities.ContainsKey(query.EntityId))
            {
                throw new Exception($"查询的实体不存在: {query.EntityId}");
            }
            
            var entity = _entities[query.EntityId];
            
            switch (query.Type)
            {
                case "EntityHealth":
                    return entity.GetComponent<HealthComponent>().CurrentHealth;
                    
                case "EntityPosition":
                    var pos = entity.GetComponent<TransComponent>().Position;
                    return new { x = (float)pos.x, y = (float)pos.y, z = (float)pos.z };
                    
                case "EntityIsAlive":
                    return entity.GetComponent<HealthComponent>().CurrentHealth > 0;
                    
                case "EntityAction":
                    var actionComp = entity.GetComponent<ActionComponent>();
                    return actionComp.CurrentAction != null ? "CastingSkill" : "Idle";
                    
                case "EntityDistance":
                    if (!string.IsNullOrEmpty(query.TargetId))
                    {
                        if (!_entities.ContainsKey(query.TargetId))
                        {
                            throw new Exception($"目标实体不存在: {query.TargetId}");
                        }
                        
                        var targetEntity = _entities[query.TargetId];
                        var pos1 = entity.GetComponent<TransComponent>().Position;
                        var pos2 = targetEntity.GetComponent<TransComponent>().Position;
                        return (float)TSVector.Distance(pos1, pos2);
                    }
                    return 0f;
                    
                default:
                    throw new NotImplementedException($"查询类型未实现: {query.Type}");
            }
        }
        
        /// <summary>
        /// 验证预期输出
        /// </summary>
        private bool VerifyExpectedOutputs(System.Collections.Generic.Dictionary<string, object> queryResults, System.Collections.Generic.Dictionary<string, object> expectedOutputs)
        {
            bool allMatch = true;
            
            foreach (var expected in expectedOutputs)
            {
                var key = expected.Key;
                var expectedValue = expected.Value;
                
                if (!queryResults.ContainsKey(key))
                {
                    _output.WriteLine($"      ❌ 缺少查询结果: {key}");
                    allMatch = false;
                    continue;
                }
                
                var actualValue = queryResults[key];
                
                // 支持范围验证（min/max）- JSON反序列化为 Newtonsoft.Json.Linq.JObject
                if (expectedValue is Newtonsoft.Json.Linq.JObject jobj && jobj["min"] != null)
                {
                    int actual = Convert.ToInt32(actualValue);
                    int min = jobj["min"].ToObject<int>();
                    int max = jobj["max"] != null ? jobj["max"].ToObject<int>() : int.MaxValue;
                    
                    if (actual < min || actual > max)
                    {
                        _output.WriteLine($"      ❌ {key}: 期望 [{min}, {max}], 实际 {actual}");
                        allMatch = false;
                    }
                    else
                    {
                        _output.WriteLine($"      ✓ {key}: {actual} (在范围内)");
                    }
                }
                // 精确匹配
                else
                {
                    // 类型转换处理
                    string expectedStr = expectedValue?.ToString();
                    string actualStr = actualValue?.ToString();
                    
                    if (expectedStr != actualStr)
                    {
                        _output.WriteLine($"      ❌ {key}: 期望 {expectedValue}, 实际 {actualValue}");
                        allMatch = false;
                    }
                    else
                    {
                        _output.WriteLine($"      ✓ {key}: {actualValue}");
                    }
                }
            }
            
            return allMatch;
        }
    }
}

