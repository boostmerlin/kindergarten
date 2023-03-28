
using System;
using System.IO;

public enum PyType
{
    YUNMU,
    DAN_YUNMU,
    FU_YUNMU,
    BI_YUNMU,
    SHENGMU,
    ZHENGTI_YINJIE,
    SANPIN_YINJIE,
}

public struct PyElement : IEquatable<PyElement>
{
    public PyElement(int id, string name, string src, string yin, string  type, string spell)
    {
        this.id = id;
        this.name = name;
        this.src = src;
        this.yin = yin;
        this.type = type;
        this.spell = spell;
    }

    public readonly int id;
    public readonly string name;
    public readonly string src;
    public readonly string yin;
    public readonly string type;
    public readonly string spell;

    public override string ToString()
    {
        return $"id={id}, name={name}, src={src}, yin={yin}, type={type}, spell={spell}";
     }

    public override bool Equals(object obj)
    {
        return obj is PyElement data && Equals(data);
    }

    public bool Equals(PyElement other)
    {
        return name == other.name;
    }

    public override int GetHashCode()
    {
        return id;
    }

    public string Uri
    {
        get
        {
            string ret = src;
            if (src.StartsWith("./"))
            {
                ret = src.Substring(2);
            }
            var p = Path.ChangeExtension(ret, "");
            return p.Substring(0, p.Length -1);
        }
    }

    public static bool operator ==(PyElement left, PyElement right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PyElement left, PyElement right)
    {
        return !(left == right);
    }
}
