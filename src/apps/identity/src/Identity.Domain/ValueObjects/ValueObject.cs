namespace Identity.Domain.ValueObjects;

/// <summary>
/// Clase base para todos los Value Objects del dominio.
///
/// En C#, por defecto dos objetos son iguales solo si son
/// la misma instancia en memoria (igualdad referencial).
///
/// Los Value Objects necesitan igualdad por valor:
///   var a = Email.Create("juan@test.com");
///   var b = Email.Create("juan@test.com");
///   a == b → true (mismo valor, aunque sean instancias distintas)
///
/// GetEqualityComponents() define qué propiedades participan
/// en la comparación. Cada subclase lo implementa.
/// </summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(((ValueObject)obj).GetEqualityComponents());
    }

    public override int GetHashCode() =>
        GetEqualityComponents().Select(x => x?.GetHashCode() ?? 0).Aggregate((x, y) => x ^ y);

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
