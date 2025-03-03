﻿using Dapper;
using CertainCourse.OrderService.DataAccess.Postgres.Common.Sharded;
using CertainCourse.OrderService.DataAccess.Postgres.Common.Sharded.ShardingRules;
using CertainCourse.OrderService.DataAccess.Postgres.Repositories.Interfaces;
using CertainCourse.OrderService.Infrastructure.DataAccess.Postgres.Common.Sharded;
using CertainCourse.OrderService.Infrastructure.DataAccess.Postgres.Dals;

namespace CertainCourse.OrderService.DataAccess.Postgres.Repositories.Sharded;

internal sealed class RegionShardedDbRepository : BaseShardedRepository, IRegionDbRepository
{
    private const string FIELDS = "id, name, storage_id";
    private const string TABLE = $"{BucketHelper.BucketPlaceholder}.regions";

    public RegionShardedDbRepository(
        IShardingRule<int> intShardingRule,
        IShardConnectionFactory connectionFactory,
        IShardingRule<string> stringShardingRule,
        IShardingRule<long> longShardingRule) : base(
        intShardingRule,
        connectionFactory,
        stringShardingRule,
        longShardingRule)
    {
    }

    public async Task<RegionDal> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        const string query = $"""
                                  select {FIELDS}
                                  from {TABLE}
                                  where id = @id;
                              """;
        
        var command = new CommandDefinition(
            query,
            new { id },
            cancellationToken: cancellationToken);
        
        await using var connection = GetRandomConnection();
        var region = await connection.QuerySingleOrDefaultAsync<RegionDal>(command);
        
        return region ?? throw new KeyNotFoundException($"No region with this id: {id}");
    }

    public async Task<IReadOnlyDictionary<int, RegionDal>> GetByIdsAsync(IReadOnlyCollection<int> regionIds,
        CancellationToken cancellationToken)
    {
        const string query = $"""
                                  select {FIELDS}
                                  from {TABLE}
                                  where id = any(@regionIds);
                              """;
        
        var command = new CommandDefinition(
            query,
            new { regionIds },
            cancellationToken: cancellationToken);
        
        await using var connection = GetRandomConnection();
        var regions = await connection.QueryAsync<RegionDal>(command);

        return regions.ToDictionary(r => r.Id, r => r);
    }

    public async Task<IReadOnlyCollection<RegionDal>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string query = $"""
                                  select {FIELDS}
                                  from {TABLE};
                              """;

        var command = new CommandDefinition(
            query,
            cancellationToken: cancellationToken);
        
        await using var connection = GetRandomConnection();
        var regions = await connection.QueryAsync<RegionDal>(command);
        return regions.ToArray();
    }

    public async Task<RegionDal> FindByNameAsync(string regionName, CancellationToken cancellationToken)
    {
        const string query = $"""
                                  select {FIELDS}
                                  from {TABLE}
                                  where name = @regionName;
                              """;
        
        var command = new CommandDefinition(
            query,
            new { regionName },
            cancellationToken: cancellationToken);
        
        await using var connection = GetRandomConnection();
        var region = await connection.QuerySingleOrDefaultAsync<RegionDal>(command);
        
        return region ?? throw new KeyNotFoundException($"No region with this name: {regionName}");
    }

    public async Task<bool> IsRegionExistAsync(int id, CancellationToken cancellationToken)
    {
        const string query = $"""
                                  SELECT EXISTS(
                                    SELECT 1 FROM {TABLE} 
                                             WHERE id=@id)
                              """;
        
        var command = new CommandDefinition(
            query,
            new { id },
            cancellationToken: cancellationToken);
        
        await using var connection = GetRandomConnection();
        return await connection.QuerySingleAsync<bool>(command);
    }
}