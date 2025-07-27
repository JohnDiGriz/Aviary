using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AviaryModules.Helpers;

public static class PrecompilationHelpers
{
    public class FullAccessor<TEntity, TMember>(Func<TEntity, TMember> get, Action<TEntity, TMember> set)
    {
        public Func<TEntity, TMember> Get { get; } = get;

        public Action<TEntity, TMember> Set { get; } = set;
    }

    public static FullAccessor<TEntity, TMember> BuildFullAccessor<TEntity, TMember>(this string memberName)
    {
        var memberInfo = typeof(TEntity).GetMember(memberName, MemberTypes.Field | MemberTypes.Property,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault();
        return memberInfo is not null ? memberInfo.BuildFullAccessor<TEntity, TMember>()
            : throw new ApplicationException("Import error: can't find a property for member " + memberName);
    }

    public static Func<TEntity, TMember> BuildGet<TEntity, TMember>(this string memberName)
    {
        var memberInfo = typeof(TEntity).GetMember(memberName, MemberTypes.Field | MemberTypes.Property,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault();
        return memberInfo is not null ? memberInfo.BuildGet<TEntity, TMember>()
            : throw new ApplicationException("Import error: can't find a property for member " + memberName);
    }

    public static Action<TEntity, TMember> BuildSet<TEntity, TMember>(this string memberName)
    {
        var memberInfo = typeof(TEntity).GetMember(memberName, MemberTypes.Field | MemberTypes.Property,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault();
        return memberInfo is not null ? memberInfo.BuildSet<TEntity, TMember>()
            : throw new ApplicationException("Import error: can't find a property for member " + memberName);
    }

    private static FullAccessor<TEntity, TMember> BuildFullAccessor<TEntity, TMember>(this MemberInfo memberInfo)
    {
        return new FullAccessor<TEntity, TMember>(
            memberInfo.BuildGet<TEntity, TMember>(),
            memberInfo.BuildSet<TEntity, TMember>()
        );
    }

    private static Func<TEntity, TRet> BuildGet<TEntity, TRet>(this MemberInfo memberInfo)
    {
        var declaringType = memberInfo.DeclaringType;
        var parameterExpression = declaringType is not null
            ? Expression.Parameter(declaringType, "t")
            : throw new ApplicationException("Import error: can't find a declaring type for member " + memberInfo.Name);
        return Expression
            .Lambda<Func<TEntity, TRet>>(Expression.MakeMemberAccess(parameterExpression, memberInfo),
                parameterExpression).Compile();
    }

    private static Action<TEntity, TMember> BuildSet<TEntity, TMember>(this MemberInfo memberInfo)
    {
        var declaringType = memberInfo.DeclaringType;
        var entityParam = declaringType is not null ?
            Expression.Parameter(declaringType, "t"): throw new ApplicationException(
            "Import error: can't find a declaring type for property " + memberInfo.Name);
        var memberAccess = Expression.MakeMemberAccess(entityParam, memberInfo);
        var newValue = Expression.Parameter(typeof(TMember), "p");
        return Expression.Lambda<Action<TEntity, TMember>>(
            Expression.Assign(memberAccess, newValue),
            entityParam,
            newValue).Compile();
    }
}