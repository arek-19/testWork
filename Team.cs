using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(UniqueId))]
[RequireComponent(typeof(SerializedStaticObject))]
[AddComponentMenu("Unit/Team")]
public class Team : MonoBehaviour
{
  private readonly static string className = "Team";

  [Tooltip("Enemy teams for this team.")]
  [SerializeField]
  private GameObject[] enemyTeams;

  [Tooltip("Max unit can be add to this team.")]
  [SerializeField]
  private int maxUnits = int.MaxValue;

  private int _numberUnits = 0;
  private Dictionary<string, GameObject> _objectPrefabs = new Dictionary<string, GameObject>();

  private void Start()
  {
    if (GetComponents<Team>().Length > 1)
    {
      Debug.LogError("Too many Team components in : " + gameObject.name + "! Can be only one!");
      Destroy(this);
      return;
    }
  }
  public bool CanAddUnit
  {
    get { return _numberUnits < maxUnits; }
  }

  public void AddUnit(GameObject unit)
  {
    if (!CanAddUnit)
    {
      Debug.LogWarning("Too many units in team. Can't add new.");
      return;
    }
    unit.transform.parent = transform;
    _numberUnits++;
  }

  public void SaveStaticObject(SerializedStaticObject.SendMessageStruct param)
  {
    param.SetComponentResult(className, false);
    var id = GetComponent<UniqueId>().ID;
    var units = GetComponentsInChildren<SerializedDynamicObject>();
    if (!DictionaryTools<string, object>.TryToAdd(ref param.dictionary, className + "[" + id + "]._numberUnits", _numberUnits, true))
      return;

    var teamPrefix = className + "[" + id + "]";

    int index = 0;
    foreach (var unit in units)
      if (!AddUnitToDictionary(unit, teamPrefix, index++, ref param.dictionary))
        return;

    param.SetComponentResult(className, DictionaryTools<string, object>.TryToAdd(ref param.dictionary, className + "[" + id + "].numActiveUnits", index, true));
  }

  private bool AddUnitToDictionary(SerializedDynamicObject unit, string teamPrefix, int index, ref Dictionary<string, object> dictionary)
  {
    var prefix = teamPrefix + ".object[" + index + "]";
    if (!DictionaryTools<string, object>.TryToAdd(ref dictionary, prefix + ".prefabName", unit.PrefabName, true))
      return false; 
    return unit.SaveObject(new KeyValuePair<string, Dictionary<string, object>>(prefix, dictionary));
  }

  public void LoadStaticObject(SerializedStaticObject.SendMessageStruct param)
  {
    param.SetComponentResult(className, false);
    RemoveAllDynamicChild();
    var id = GetComponent<UniqueId>().ID;
    bool isSuccess = false;
    _numberUnits = (int)DictionaryTools<string, object>.TryToGet(ref param.dictionary, className + "[" + id + "]._numberUnits", _numberUnits, out isSuccess, true);
    if (!isSuccess)
      return;

    var numActiveUnits = (int)DictionaryTools<string, object>.TryToGet(ref param.dictionary, className + "[" + id + "].numActiveUnits", 0, out isSuccess, true);
    if (!isSuccess)
      return;

    var teamPrefix = className + "[" + id + "]";
    for (var index = 0;index < numActiveUnits; ++index)
    {
      SerializedDynamicObject unit = null;
      if (!GetUnitFromDictionary(out unit, teamPrefix, index, ref param.dictionary))
        return ;

      unit.transform.parent = transform;
    };
    param.SetComponentResult(className, true);
  }

  private bool GetUnitFromDictionary(out SerializedDynamicObject unit, string teamPrefix, int index, ref Dictionary<string, object> dictionary)
  {
    unit = null;
    bool isSuccess = false;
    var prefix = teamPrefix + ".object[" + index + "]";
    var prefabName = (string)DictionaryTools<string, object>.TryToGet(ref dictionary, prefix + ".prefabName", "", out isSuccess, true);
    if (!isSuccess)
      return false;

    var obj = CreateObject(prefabName);
    if (!obj)
      return false;
    unit = obj.GetComponent<SerializedDynamicObject>();
    if (!unit)
      return false;
    return unit.LoadObject(new KeyValuePair<string, Dictionary<string, object>>(prefix, dictionary));
  }

  private void RemoveAllDynamicChild()
  {
    var objects = GetComponentsInChildren<SerializedDynamicObject>();
    foreach (var obj in objects)
      Destroy(obj.gameObject);
  }

  private GameObject CreateObject(string prefabName)
  {
    if (!_objectPrefabs.TryGetValue(prefabName, out GameObject prefab))
    {
      Debug.LogError("Cen't find " + prefabName + " !");
      return null;
    }
    return Instantiate(prefab);
  }

  public void AddPrefabIfNotAddedYet(GameObject prefab)
  {
    if (_objectPrefabs.ContainsKey(prefab.name))
      return;
    _objectPrefabs.Add(prefab.name, prefab);
  }
  
  public Transform FindNearEnemy(Vector3 position, float maxDistance)
  {
    var minDistanceSqr = maxDistance * maxDistance;
    Transform result = null;
    foreach (var team in enemyTeams)
      TransformTools.FindNearChildToPos(team.transform, position, ref minDistanceSqr, ref result);

    return result;
  }
}
