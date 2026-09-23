// File: QrVerifyRequest.cs
// Purpose: the scanned qr text the operator app sends before finalising a transfer
// Author: T.H.Nimnath Nadushka

namespace SolarMicrogrid.Api.Models;

public class QrVerifyRequest
{
    public string QrData { get; set; } = "";
}
