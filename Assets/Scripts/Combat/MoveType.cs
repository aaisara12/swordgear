using System;

public readonly struct MoveType : IEquatable<MoveType>
{
    public readonly Element Element;
    public readonly AttackKind Kind;

    public MoveType(Element element, AttackKind kind)
    {
        Element = element;
        Kind = kind;
    }

    public bool Equals(MoveType other) => Element == other.Element && Kind == other.Kind;
    public override bool Equals(object obj) => obj is MoveType other && Equals(other);
    public override int GetHashCode() => HashCode.Combine((int)Element, (int)Kind);
    public static bool operator ==(MoveType a, MoveType b) => a.Equals(b);
    public static bool operator !=(MoveType a, MoveType b) => !a.Equals(b);
    public override string ToString() => $"({Element}, {Kind})";
}
