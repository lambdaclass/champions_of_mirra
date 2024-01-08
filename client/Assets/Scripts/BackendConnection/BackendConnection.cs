using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public static class BackendConnection
{
    public static IEnumerator GetAvailableUnits(
        string playerDeviceId,
        Action<List<UnitDTO>> successCallback,
        Action<string> userNotFoundCallback
    )
    {
        string url = $"http://localhost:4000/users/{playerDeviceId}/get_units";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("Content-Type", "application/json");
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    if (webRequest.downloadHandler.text.Contains("NOT_FOUND"))
                    {
                        userNotFoundCallback?.Invoke("USER_NOT_FOUND");
                    }
                    else
                    {
                        List<UnitDTO> units = JsonConvert.DeserializeObject<List<UnitDTO>>(
                            webRequest.downloadHandler.text
                        );
                        units = units.OrderByDescending(unit => unit.selected).ThenByDescending(unit => unit.slot).ThenByDescending(unit => unit.level).ToList();
                        successCallback?.Invoke(units);
                    }
                    webRequest.Dispose();
                    break;
                default:
                    HandleUnsuccessfulResponse(webRequest);
                    break;
            }
        }
    }

    public static IEnumerator SelectUnit(
        string playerDeviceId,
        string unitId,
        int slot,
        Action<string> inexistentUserCallback
    )
    {
        string url = $"http://localhost:4000/users/{playerDeviceId}/select_unit/{unitId}";
        string parametersJson = "{\"unit_id\":\"" + unitId + "\"}";
        byte[] byteArray = Encoding.UTF8.GetBytes(parametersJson);
        using (UnityWebRequest webRequest = UnityWebRequest.Put(url, byteArray))
        {
            webRequest.SetRequestHeader("Content-Type", "application/json");
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    if (webRequest.downloadHandler.text.Contains("INEXISTENT_USER"))
                    {
                        inexistentUserCallback?.Invoke(webRequest.downloadHandler.text);
                    }
                    break;
                default:
                    HandleUnsuccessfulResponse(webRequest);
                    break;
            }
        }
    }

    public static IEnumerator UnselectUnit(
        string playerDeviceId,
        string unitId,
        Action<string> inexistentUserCallback
    )
    {
        string url = $"http://localhost:4000/users/{playerDeviceId}/unselect_unit/{unitId}";
        string parametersJson = "{\"unit_id\":\"" + unitId + "\"}";
        byte[] byteArray = Encoding.UTF8.GetBytes(parametersJson);
        using (UnityWebRequest webRequest = UnityWebRequest.Put(url, byteArray))
        {
            webRequest.SetRequestHeader("Content-Type", "application/json");
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    if (webRequest.downloadHandler.text.Contains("INEXISTENT_USER"))
                    {
                        inexistentUserCallback?.Invoke(webRequest.downloadHandler.text);
                    }
                    break;
                default:
                    HandleUnsuccessfulResponse(webRequest);
                    break;
            }
        }
    }

    public static IEnumerator GetBattleResult(
        string playerDeviceId,
        string opponentId,
        Action<string> successCallback,
        Action<string> userNotFoundCallback
    )
    {
        string url = $"http://localhost:4000/battle/{playerDeviceId}/pvp/{opponentId}";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("Content-Type", "application/json");
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    if (webRequest.downloadHandler.text.Contains("NOT_FOUND"))
                    {
                        userNotFoundCallback?.Invoke("USER_NOT_FOUND");
                    }
                    else
                    {
                        BattleResultDTO battleResult = JsonConvert.DeserializeObject<BattleResultDTO>(
                            webRequest.downloadHandler.text
                        );
                        yield return new WaitForSeconds(2);
                        successCallback?.Invoke(battleResult.winner);
                    }
                    webRequest.Dispose();
                    break;
                default:
                    HandleUnsuccessfulResponse(webRequest);
                    break;
            }
        }
    }

    public static IEnumerator GetOpponents
    (
        string playerDeviceId,
        Action<List<UserDTO>> successCallback,
        Action<string> userNotFoundCallback
    )
    {
        string url = $"http://localhost:4000/users/{playerDeviceId}/get_opponents";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("Content-Type", "application/json");
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    if (webRequest.downloadHandler.text.Contains("NOT_FOUND"))
                    {
                        userNotFoundCallback?.Invoke("USER_NOT_FOUND");
                    }
                    else
                    {
                        List<UserDTO> opponents = JsonConvert.DeserializeObject<List<UserDTO>>(
                            webRequest.downloadHandler.text
                        );
                        successCallback?.Invoke(opponents);
                    }
                    webRequest.Dispose();
                    break;
                default:
                    HandleUnsuccessfulResponse(webRequest);
                    break;
            }

        }
    }

    private static void HandleUnsuccessfulResponse(UnityWebRequest webRequest)
    {
        switch (webRequest.result)
        {
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError("Something unexpected happened");
                break;
            case UnityWebRequest.Result.ConnectionError:
                Debug.LogError("Connection Error");
                break;
            case UnityWebRequest.Result.DataProcessingError:
                Debug.LogError("Data processing error.");
                break;
            default:
                Debug.LogError("Unhandled error.");
                break;
        }
    }
}

