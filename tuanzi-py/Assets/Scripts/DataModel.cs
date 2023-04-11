using System.IO;
using UnityEngine;
using Mono.Data.SqliteClient;
using System.Data;
using System.Collections.Generic;

public class DataModel : MonoBehaviour
{
    private const string SQL_DB_NAME = "kindergarten";

    // table name
    private const string SQL_TABLE_NAME = "pinyin";
    private IDbConnection _connection = null;

    internal Dictionary<PyType, List<PyElement>> PyDatas { get; set; } = new Dictionary<PyType, List<PyElement>>();

    internal Dictionary<PyElement, List<PyElement>> PyTone = new Dictionary<PyElement, List<PyElement>>();

    private string _sqlDBLocation
    {
        get
        {
            Debug.Log("StreamingAssets: " + Application.streamingAssetsPath);
            string uri = "URI=" + new System.Uri(Path.Combine(Application.streamingAssetsPath, SQL_DB_NAME + ".db")).AbsoluteUri;
            Debug.Log($"data base uri is: {uri}");
            return uri;
         }
    }
    void Start()
    {
        SQLiteInit();
        LoadFromDB();
    }

    private void OnDestroy()
    {
        if (_connection.State != ConnectionState.Closed)
        {
            _connection.Close();
        }
        _connection = null;
    }

    void SQLiteInit()
    {
        _connection = SqliteClientFactory.Instance.CreateConnection();
        _connection.ConnectionString = _sqlDBLocation;
        _connection.Open();
        IDbCommand cmd =  _connection.CreateCommand();

        cmd.CommandText = "SELECT name FROM sqlite_master WHERE name='" + SQL_TABLE_NAME + "'";
        var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            Debug.LogError("NO SQLite table " + SQL_TABLE_NAME);
            throw new System.Exception("table not exist.");
        }

        Debug.Log("table: " + reader.GetString(0));
        reader.Close();

        _connection.Close();
    }

    List<PyElement> ReadPyData(IDbCommand cmd, string where, bool log=false)
    {
        List<PyElement> list = new List<PyElement>();
        cmd.CommandText = $"SELECT * FROM {SQL_TABLE_NAME} WHERE {where}";
        using (var reader = cmd.ExecuteReader())
        {
            while(reader.Read())
            {
                   var d = new PyElement(
                       reader.GetInt32(0),
                       reader.GetString(1),
                       reader.GetString(2),
                       reader.GetString(3),
                       reader.GetString(4),
                       reader.GetString(5)
                ) ;
                list.Add(d);
            }
            cmd.ExecuteReader();
        }

        if(log)
        {
            list.ForEach(d => Debug.Log(d));
        }

        return list;
    }

    void LoadFromDB()
    {
        _connection.Open();

        using (IDbCommand cmd = _connection.CreateCommand())
        {
            var yunmu = ReadPyData(cmd, "type='韵母'");

            var dan_yunmu = yunmu.FindAll(d => d.name.Length == 1);

            PyDatas.Add(PyType.YUNMU, yunmu);
            PyDatas.Add(PyType.DAN_YUNMU, dan_yunmu);

            var fu_yunmu = yunmu.FindAll(d => d.name.Length > 1 && !d.name.Contains("n"));

            PyDatas.Add(PyType.FU_YUNMU, fu_yunmu);

            var bi_yunmu = yunmu.FindAll(d => d.name.Length > 1 && d.name.Contains("n"));

            PyDatas.Add(PyType.BI_YUNMU, bi_yunmu);

            string where = "type='声母'";
            var shengmu = ReadPyData(cmd, where);
            PyDatas.Add(PyType.SHENGMU, shengmu);
            
            where = "type='整体认读音节'";
            var zhengti = ReadPyData(cmd, where);
            PyDatas.Add(PyType.ZHENGTI_YINJIE, zhengti);

            
            where = "type='三拼音节'";
            var sanpin = ReadPyData(cmd, where);
            PyDatas.Add(PyType.SANPIN_YINJIE, sanpin);

            //音调,在韵母和整体认读音节上

            foreach (var item in yunmu)
            {
                where = string.Format("name IN ('{0}', '{1}', '{2}', '{3}')", item.name + "1", item.name + "2", item.name + "3", item.name + "4");
                PyTone.Add(item, ReadPyData(cmd, where));
            }
        }

        _connection.Close();
    }
}
