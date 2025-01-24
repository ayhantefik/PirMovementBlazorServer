using Microsoft.AspNetCore.Components;
using PirMovementBlazorServer.Infrastructure;
using PirMovementBlazorServer.Models;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace PirMovementBlazorServer.Services;

// RealTime Movement list updating service

public class MovementListService
{
    private readonly IConfiguration _config;
    private List<Movement> _currentMovements = new();
    public event Action? OnChange;

    public List<Movement> CurrentMovementList() => _currentMovements;

    public MovementListService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<List<Movement>> UpdateMovements()
    {
        var websiteConfig = new WebsiteConfig(_config);

        HttpClient httpClient = new HttpClient();
        var newMovements = await httpClient.GetFromJsonAsync<List<Movement>>($"{websiteConfig.Url}api/movements");

        if (newMovements != null)
        {
            _currentMovements.Clear();
            _currentMovements.AddRange(newMovements);
        }
        return _currentMovements;
    }

    public void AddMovement(Movement move)
    {
        _currentMovements.RemoveAt(_currentMovements.Count - 1);
        _currentMovements.Add(move);
        _currentMovements = _currentMovements.OrderByDescending(x => x.MovementTime).ToList();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}