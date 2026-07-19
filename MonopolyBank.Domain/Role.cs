namespace MonopolyBank.Domain;

public abstract class Role(bool hasRole, string roleName)
{
    protected void EnsureHasRole()
    {
        if (!hasRole)
            throw new MissingRoleException(roleName);
    }
}
