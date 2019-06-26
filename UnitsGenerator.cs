using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(UniqueId))]
[RequireComponent(typeof(SerializedStaticObject))]
[AddComponentMenu("Unit/Units Generator")]
public class UnitsGenerator: MonoBehaviour
{
  private readonly static string className = "UnitsGenerator";
  [Tooltip("The team to which you add units.")]
  [SerializeField]
  private Team team;

  [Tooltip("List of unit Prefabs [min 1].")]
  [SerializeField]
  private GameObject[] unitPrefabs;

  [Tooltip("The maximum number of unit to be created.")]
  [SerializeField]
  private int maxUnits = int.MaxValue;

  [Tooltip("Time between creating a unit (in seconds).")]
  [SerializeField]
  private RangeFloat emissionTimeRange = new RangeFloat(10.0f, 10.0f);

  [Tooltip("Remove this GameObject after emit last Unit. If not set remove only this component.")]
  [SerializeField]
  private bool removeGameObject = false;

  private float _timeToEmission = 0.0f;
  private int _numEmited = 0;

  void Start()
  {
    if (GetComponents<UnitsGenerator>().Length>1)
    {
      Debug.LogError("Too many UnitsGenerator components in : " + gameObject.name + "! Can be only one!");
      Destroy(this);
      return;
    }

    if (!team)
    {
      Debug.LogError("Team not set. Object name : " + gameObject.name + "! Component was removed!");
      Destroy(this);
      return;
    }

    if (unitPrefabs.Length == 0)
    {
      Debug.LogError("Unit Prefabs must have at last one element. Object name : " + gameObject.name + "! Component was removed!");
      Destroy(this);
      return;
    }
    foreach (var prefab in unitPrefabs)
      team.AddPrefabIfNotAddedYet(prefab);
  }

  void Update()
  {
    if (_numEmited >= maxUnits || !team.CanAddUnit)
    {
      Clear();
      return;
    }
    _timeToEmission -= Time.deltaTime;
    if (_timeToEmission <= 0)
    {
      CreateNewUnit();
      _timeToEmission = UnityEngine.Random.Range(emissionTimeRange.start, emissionTimeRange.end);
    }
  }

  private void Clear()
  {
    if (removeGameObject)
      Destroy(gameObject);
    else
      Destroy(this);
  }

  private void CreateNewUnit()
  {
    var unit = Instantiate(unitPrefabs[UnityEngine.Random.Range(0, unitPrefabs.Length)]);
    unit.transform.position = transform.position;
    unit.transform.position += new Vector3(UnityEngine.Random.Range(-transform.localScale.x, transform.localScale.x), UnityEngine.Random.Range(-transform.localScale.y, transform.localScale.y), UnityEngine.Random.Range(-transform.localScale.z, transform.localScale.z));
    team.AddUnit(unit);
    _numEmited++;
  }

  public void SaveStaticObject(SerializedStaticObject.SendMessageStruct param)
  {
    var id = GetComponent<UniqueId>().ID;
    param.SetComponentResult(className, DictionaryTools<string, object>.TryToAdd(ref param.dictionary, className + "[" + id + "]._numEmited", _numEmited, true));
  }

  public void LoadStaticObject(SerializedStaticObject.SendMessageStruct param)
  {
    var isSuccess = false;
    var id = GetComponent<UniqueId>().ID;
    _numEmited = (int)DictionaryTools<string, object>.TryToGet(ref param.dictionary, className + "[" + id + "]._numEmited", _numEmited, out isSuccess, false);
    if (!isSuccess)
      Clear();
    param.SetComponentResult(className, true);
  }

  [ExecuteInEditMode]
  void OnValidate()
  {
    if (emissionTimeRange.start < 0.0f)
      emissionTimeRange.start = 0.0f;

    if (emissionTimeRange.length < 0.0f)
      emissionTimeRange.length = 0.0f;
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.green;
    Gizmos.DrawCube(transform.position, transform.localScale);
    UnityEditor.Handles.color = Color.green;
    UnityEditor.Handles.Label(transform.position + Vector3.up * (transform.localScale.y + 0.5f), name);
  }
}
