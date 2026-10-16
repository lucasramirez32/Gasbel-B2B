using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Gasbel.Application.Interfaces;
using Gasbel.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gasbel.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration? _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(HttpClient httpClient, ILogger<EmailService> logger, IConfiguration? configuration = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<EmailDispatchResult> SendQuoteEmailsAsync(
        Quote quote,
        CommercialSettings settings,
        CancellationToken cancellationToken = default)
    {
        bool businessSent = false;
        bool customerSent = false;
        string? businessError = null;
        string? customerError = null;
        string? previewPath = null;

        var culture = new CultureInfo("es-AR");
        var shortId = quote.Id.ToString()[..8].ToUpperInvariant();
        var dateFormatted = quote.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", culture);

        // Número de WhatsApp de Gasbel para los botones del correo
        var gasbelWhatsApp = settings.UseTestingNumber
            ? (!string.IsNullOrWhiteSpace(settings.WhatsAppTesting) ? settings.WhatsAppTesting : "2613890532")
            : (!string.IsNullOrWhiteSpace(settings.WhatsAppProduction) ? settings.WhatsAppProduction : "2616799084");

        // Guardar copia HTML en disco para vista previa / depuración
        try
        {
            var emailDir = Path.Combine(Directory.GetCurrentDirectory(), "data", "emails");
            if (!Directory.Exists(emailDir))
            {
                Directory.CreateDirectory(emailDir);
            }
            previewPath = Path.Combine(emailDir, $"cotizacion_{shortId}.html");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo asegurar el directorio data/emails");
        }

        // 1. Enviar correo al comercio (Gasbel)
        if (settings.SendEmailToBusiness && !string.IsNullOrWhiteSpace(settings.NotificationEmail))
        {
            var businessHtml = BuildBusinessEmailHtml(quote, settings, shortId, dateFormatted, gasbelWhatsApp);
            if (previewPath != null)
            {
                try
                {
                    await File.WriteAllTextAsync(previewPath, businessHtml, cancellationToken);
                }
                catch { }
            }

            var subject = $"🔔 Nueva Cotización #{shortId} - {quote.CustomerName} (${quote.Total:N2})";
            var result = await DispatchEmailAsync(
                toEmail: settings.NotificationEmail,
                subject: subject,
                htmlBody: businessHtml,
                settings: settings,
                cancellationToken: cancellationToken);

            businessSent = result.Success;
            businessError = result.ErrorMessage;
        }

        // 2. Enviar comprobante al cliente (si indicó email y la opción está habilitada)
        if (settings.SendEmailToCustomer && !string.IsNullOrWhiteSpace(quote.CustomerEmail))
        {
            var customerHtml = BuildCustomerEmailHtml(quote, settings, shortId, dateFormatted, gasbelWhatsApp);
            var subject = $"Tu Solicitud de Cotización #{shortId} en Gasbel Descartables";

            var result = await DispatchEmailAsync(
                toEmail: quote.CustomerEmail,
                subject: subject,
                htmlBody: customerHtml,
                settings: settings,
                cancellationToken: cancellationToken);

            customerSent = result.Success;
            customerError = result.ErrorMessage;
        }

        return new EmailDispatchResult(
            BusinessEmailSent: businessSent,
            CustomerEmailSent: customerSent,
            BusinessError: businessError,
            CustomerError: customerError,
            PreviewHtmlPath: previewPath
        );
    }

    private async Task<(bool Success, string? ErrorMessage)> DispatchEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CommercialSettings settings,
        CancellationToken cancellationToken)
    {
        var apiKey = !string.IsNullOrWhiteSpace(settings.ResendApiKey)
            ? settings.ResendApiKey
            : (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RESEND_API_KEY"))
                ? Environment.GetEnvironmentVariable("RESEND_API_KEY")
                : _configuration?["Resend:ApiKey"]);

        // Modo Resend API
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                var fromName = string.IsNullOrWhiteSpace(settings.SenderName) ? "Gasbel Descartables" : settings.SenderName;
                var fromAddress = string.IsNullOrWhiteSpace(settings.SenderEmail) ? "cotizaciones@gasbel.com.ar" : settings.SenderEmail;

                var payload = new
                {
                    from = $"{fromName} <{fromAddress}>",
                    to = new[] { toEmail },
                    subject = subject,
                    html = htmlBody
                };

                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Correo enviado vía Resend a {Recipient} con asunto '{Subject}'", toEmail, subject);
                    return (true, null);
                }

                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);

                // Si el dominio gasbel.com.ar aún no completó la verificación DNS en Resend,
                // intentamos automáticamente con el remitente de prueba oficial (onboarding@resend.dev)
                if ((errorText.Contains("not verified", StringComparison.OrdinalIgnoreCase) || errorText.Contains("validation_error", StringComparison.OrdinalIgnoreCase))
                    && !fromAddress.Contains("resend.dev", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Dominio aún no verificado en Resend. Reintentando con onboarding@resend.dev para pruebas...");
                    using var retryRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
                    retryRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                    var retryPayload = new
                    {
                        from = $"{fromName} <onboarding@resend.dev>",
                        to = new[] { toEmail },
                        subject = subject,
                        html = htmlBody
                    };
                    retryRequest.Content = new StringContent(JsonSerializer.Serialize(retryPayload), Encoding.UTF8, "application/json");
                    var retryResponse = await _httpClient.SendAsync(retryRequest, cancellationToken);
                    if (retryResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Correo de prueba entregado con éxito vía onboarding@resend.dev a {Recipient}", toEmail);
                        return (true, null);
                    }
                }

                _logger.LogWarning("Error en Resend API ({StatusCode}): {Error}", response.StatusCode, errorText);
                return (false, $"Resend ({response.StatusCode}): {errorText}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al enviar correo vía Resend a {Recipient}", toEmail);
                return (false, ex.Message);
            }
        }

        // Modo SMTP tradicional
        if (string.Equals(settings.EmailProvider, "Smtp", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            try
            {
                using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
                {
                    EnableSsl = settings.SmtpUseSsl,
                    Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPassword)
                };

                var from = new MailAddress(settings.SenderEmail, settings.SenderName);
                var to = new MailAddress(toEmail);
                using var mail = new MailMessage(from, to)
                {
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mail, cancellationToken);
                _logger.LogInformation("Correo enviado vía SMTP a {Recipient}", toEmail);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al enviar correo vía SMTP a {Recipient}", toEmail);
                return (false, ex.Message);
            }
        }

        // Modo Simulado (Desarrollo / Sin credenciales aún)
        _logger.LogInformation("Simulación de correo a {Recipient} (Asunto: '{Subject}'). Configure ResendApiKey o SMTP para entrega real.", toEmail, subject);
        return (true, null);
    }

    private static string BuildBusinessEmailHtml(
        Quote quote,
        CommercialSettings settings,
        string shortId,
        string dateFormatted,
        string gasbelWhatsApp)
    {
        var cleanCustomerPhone = CleanPhoneNumber(quote.CustomerPhone);
        var waCustomerUrl = $"https://wa.me/549{cleanCustomerPhone}?text={Uri.EscapeDataString($"Hola {quote.CustomerName}, te contactamos de Gasbel respecto a tu cotización #{shortId}.")}";

        var itemsRows = new StringBuilder();
        int index = 1;
        foreach (var item in quote.Items)
        {
            var packaging = item.PackagingSelected == "Box" ? "Bulto Cerrado" : "Unidad / Paquete";
            var productName = item.Product?.Name ?? "Artículo";
            var sku = item.Product?.Sku ?? "";

            itemsRows.Append($@"
                <tr style=""border-bottom: 1px solid #e2e8f0;"">
                    <td style=""padding: 10px; font-size: 13px; color: #1e293b;"">
                        <strong>{index}. {WebUtility.HtmlEncode(productName)}</strong>
                        {(string.IsNullOrWhiteSpace(sku) ? "" : $"<br><span style=\"font-size: 11px; color: #64748b;\">SKU: {WebUtility.HtmlEncode(sku)}</span>")}
                    </td>
                    <td style=""padding: 10px; font-size: 12px; color: #475569; text-align: center;"">{packaging}</td>
                    <td style=""padding: 10px; font-size: 13px; color: #0f172a; font-weight: bold; text-align: center;"">{item.Quantity}</td>
                    <td style=""padding: 10px; font-size: 13px; color: #475569; text-align: right;"">${item.AppliedUnitPrice:N2}</td>
                    <td style=""padding: 10px; font-size: 13px; color: #0f172a; font-weight: bold; text-align: right;"">${item.LineSubtotal:N2}</td>
                </tr>
            ");
            index++;
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Nueva Cotización #{shortId} - Gasbel B2B</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;"">
    <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f1f5f9; padding: 24px 12px;"">
        <tr>
            <td align=""center"">
                <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""max-width: 640px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.06);"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #0f172a; padding: 24px 30px; text-align: left;"">
                            <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"">
                                <tr>
                                    <td>
                                        <div style=""display: inline-block; background-color: #d97706; color: #ffffff; font-weight: 900; font-size: 11px; letter-spacing: 0.1em; padding: 4px 10px; border-radius: 6px; text-transform: uppercase;"">
                                            Notificación Comercial Interna
                                        </div>
                                        <h1 style=""color: #ffffff; margin: 8px 0 2px 0; font-size: 22px; font-weight: 800;"">GASBEL B2B</h1>
                                        <p style=""color: #94a3b8; margin: 0; font-size: 12px;"">Descartables y Perfumería al por mayor</p>
                                    </td>
                                    <td align=""right"" style=""vertical-align: middle;"">
                                        <div style=""background-color: rgba(255,255,255,0.1); border: 1px solid rgba(255,255,255,0.2); border-radius: 10px; padding: 8px 14px; text-align: right;"">
                                            <span style=""display: block; color: #fbbf24; font-size: 10px; font-weight: 700; text-transform: uppercase;"">Cotización</span>
                                            <span style=""display: block; color: #ffffff; font-size: 16px; font-weight: 900;"">#{shortId}</span>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Alert Bar -->
                    <tr>
                        <td style=""background-color: #ecfdf5; border-bottom: 1px solid #a7f3d0; padding: 14px 30px;"">
                            <p style=""margin: 0; font-size: 13px; color: #065f46; font-weight: 600;"">
                                ⚡ Se ha recibido una nueva solicitud de pedido desde el catálogo web.
                            </p>
                        </td>
                    </tr>

                    <!-- Customer Info & Action Card -->
                    <tr>
                        <td style=""padding: 24px 30px;"">
                            <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 18px; margin-bottom: 24px;"">
                                <h3 style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 800; color: #0f172a; text-transform: uppercase; letter-spacing: 0.05em;"">
                                    Datos del Cliente
                                </h3>
                                <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""4"" style=""font-size: 13px; color: #334155;"">
                                    <tr>
                                        <td width=""120"" style=""color: #64748b; font-weight: 600;"">Nombre:</td>
                                        <td><strong>{WebUtility.HtmlEncode(quote.CustomerName)}</strong></td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600;"">Teléfono / WhatsApp:</td>
                                        <td>
                                            <a href=""tel:{WebUtility.HtmlEncode(quote.CustomerPhone)}"" style=""color: #2563eb; text-decoration: none; font-weight: 600;"">
                                                {WebUtility.HtmlEncode(quote.CustomerPhone)}
                                            </a>
                                        </td>
                                    </tr>
                                    {(string.IsNullOrWhiteSpace(quote.CustomerEmail) ? "" : $@"
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600;"">Email:</td>
                                        <td><a href=""mailto:{WebUtility.HtmlEncode(quote.CustomerEmail)}"" style=""color: #2563eb; text-decoration: none;"">{WebUtility.HtmlEncode(quote.CustomerEmail)}</a></td>
                                    </tr>")}
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600;"">Fecha:</td>
                                        <td>{dateFormatted} hs</td>
                                    </tr>
                                    {(string.IsNullOrWhiteSpace(quote.Notes) ? "" : $@"
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600; vertical-align: top;"">Detalles / Entrega:</td>
                                        <td style=""color: #0f172a;"">{WebUtility.HtmlEncode(quote.Notes)}</td>
                                    </tr>")}
                                </table>

                                <!-- Quick WhatsApp Button -->
                                <div style=""margin-top: 16px; padding-top: 14px; border-top: 1px dashed #cbd5e1; text-align: center;"">
                                    <a href=""{waCustomerUrl}"" target=""_blank"" style=""display: inline-block; background-color: #25d366; color: #ffffff; text-decoration: none; font-size: 13px; font-weight: 800; padding: 10px 22px; border-radius: 10px; box-shadow: 0 2px 6px rgba(37,211,102,0.3);"">
                                        💬 Abrir WhatsApp con el Cliente
                                    </a>
                                </div>
                            </div>

                            <!-- Products Table -->
                            <h3 style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 800; color: #0f172a; text-transform: uppercase; letter-spacing: 0.05em;"">
                                Artículos Solicitados
                            </h3>
                            <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""border-collapse: collapse; margin-bottom: 20px;"">
                                <thead>
                                    <tr style=""background-color: #f1f5f9; border-bottom: 2px solid #cbd5e1;"">
                                        <th align=""left"" style=""padding: 10px; font-size: 11px; font-weight: 700; color: #475569; text-transform: uppercase;"">Producto</th>
                                        <th align=""center"" style=""padding: 10px; font-size: 11px; font-weight: 700; color: #475569; text-transform: uppercase;"">Empaque</th>
                                        <th align=""center"" style=""padding: 10px; font-size: 11px; font-weight: 700; color: #475569; text-transform: uppercase;"">Cant.</th>
                                        <th align=""right"" style=""padding: 10px; font-size: 11px; font-weight: 700; color: #475569; text-transform: uppercase;"">P. Unit.</th>
                                        <th align=""right"" style=""padding: 10px; font-size: 11px; font-weight: 700; color: #475569; text-transform: uppercase;"">Subtotal</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {itemsRows}
                                </tbody>
                            </table>

                            <!-- Totals Box -->
                            <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin-bottom: 24px;"">
                                <tr>
                                    <td width=""50%""></td>
                                    <td width=""50%"">
                                        <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 14px;"">
                                            <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""4"" style=""font-size: 13px;"">
                                                <tr>
                                                    <td style=""color: #64748b;"">Subtotal Neto:</td>
                                                    <td align=""right"" style=""font-weight: 600; color: #0f172a;"">${quote.Subtotal:N2}</td>
                                                </tr>
                                                <tr>
                                                    <td style=""color: #64748b;"">IVA (21%):</td>
                                                    <td align=""right"" style=""font-weight: 600; color: #0f172a;"">${quote.TaxAmount:N2}</td>
                                                </tr>
                                                <tr style=""border-top: 2px solid #cbd5e1;"">
                                                    <td style=""padding-top: 8px; font-size: 15px; font-weight: 800; color: #0f172a;"">TOTAL:</td>
                                                    <td align=""right"" style=""padding-top: 8px; font-size: 18px; font-weight: 900; color: #d97706;"">${quote.Total:N2}</td>
                                                </tr>
                                            </table>
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- Footer -->
                            <div style=""text-align: center; border-top: 1px solid #e2e8f0; padding-top: 18px; color: #94a3b8; font-size: 11px;"">
                                Sistema de Gestión de Cotizaciones • Gasbel Descartables y Perfumería
                            </div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string BuildCustomerEmailHtml(
        Quote quote,
        CommercialSettings settings,
        string shortId,
        string dateFormatted,
        string gasbelWhatsApp)
    {
        var cleanGasbelPhone = CleanPhoneNumber(gasbelWhatsApp);
        var waGasbelUrl = $"https://wa.me/549{cleanGasbelPhone}?text={Uri.EscapeDataString($"Hola Gasbel, te contacto para consultar/coordinar mi cotización #{shortId} generada en el catálogo.")}";

        var itemsRows = new StringBuilder();
        int index = 1;
        foreach (var item in quote.Items)
        {
            var packaging = item.PackagingSelected == "Box" ? "Bulto Cerrado" : "Unidad / Paquete";
            var productName = item.Product?.Name ?? "Artículo";

            itemsRows.Append($@"
                <tr style=""border-bottom: 1px solid #f1f5f9;"">
                    <td style=""padding: 12px 10px; font-size: 13px; color: #1e293b;"">
                        <strong>{index}. {WebUtility.HtmlEncode(productName)}</strong>
                    </td>
                    <td style=""padding: 12px 10px; font-size: 12px; color: #64748b; text-align: center;"">{packaging}</td>
                    <td style=""padding: 12px 10px; font-size: 13px; color: #0f172a; font-weight: bold; text-align: center;"">{item.Quantity}</td>
                    <td style=""padding: 12px 10px; font-size: 13px; color: #475569; text-align: right;"">${item.AppliedUnitPrice:N2}</td>
                    <td style=""padding: 12px 10px; font-size: 13px; color: #0f172a; font-weight: bold; text-align: right;"">${item.LineSubtotal:N2}</td>
                </tr>
            ");
            index++;
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Comprobante de Cotización #{shortId} - Gasbel Descartables</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;"">
    <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f8fafc; padding: 24px 12px;"">
        <tr>
            <td align=""center"">
                <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""max-width: 640px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 14px rgba(0,0,0,0.06);"">
                    
                    <!-- Header Branding -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding: 30px; text-align: left;"">
                            <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"">
                                <tr>
                                    <td>
                                        <span style=""display: inline-block; background-color: #f59e0b; color: #0f172a; font-weight: 800; font-size: 10px; letter-spacing: 0.1em; padding: 3px 8px; border-radius: 4px; text-transform: uppercase; margin-bottom: 6px;"">
                                            Distribución Mayorista
                                        </span>
                                        <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: 900; letter-spacing: -0.02em;"">GASBEL</h1>
                                        <p style=""color: #cbd5e1; margin: 4px 0 0 0; font-size: 12px;"">Descartables & Perfumería • Mendoza</p>
                                    </td>
                                    <td align=""right"" style=""vertical-align: middle;"">
                                        <div style=""background-color: rgba(255,255,255,0.08); border: 1px solid rgba(255,255,255,0.15); border-radius: 12px; padding: 10px 16px; text-align: right;"">
                                            <span style=""display: block; color: #fbbf24; font-size: 11px; font-weight: 700; text-transform: uppercase;"">Presupuesto</span>
                                            <span style=""display: block; color: #ffffff; font-size: 17px; font-weight: 900;"">#{shortId}</span>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Greeting & Summary -->
                    <tr>
                        <td style=""padding: 26px 30px 16px 30px;"">
                            <h2 style=""margin: 0 0 8px 0; font-size: 18px; color: #0f172a; font-weight: 800;"">
                                ¡Hola {WebUtility.HtmlEncode(quote.CustomerName)}! 👋
                            </h2>
                            <p style=""margin: 0 0 16px 0; font-size: 14px; color: #475569; line-height: 1.5;"">
                                Muchas gracias por interesarte en nuestros productos. Registramos tu solicitud de cotización correctamente. A continuación tenés el comprobante detallado con los artículos seleccionados y precios vigentes.
                            </p>

                            <!-- Call to Action Banner -->
                            <div style=""background: #ecfdf5; border: 1px solid #6ee7b7; border-radius: 12px; padding: 16px; text-align: center; margin-bottom: 24px;"">
                                <p style=""margin: 0 0 10px 0; font-size: 13px; font-weight: 700; color: #065f46;"">
                                    ¿Querés coordinar el pago o la entrega ahora mismo?
                                </p>
                                <a href=""{waGasbelUrl}"" target=""_blank"" style=""display: inline-block; background-color: #10b981; color: #ffffff; text-decoration: none; font-size: 13px; font-weight: 800; padding: 11px 24px; border-radius: 10px; box-shadow: 0 2px 8px rgba(16,185,129,0.3);"">
                                    💬 Coordinar por WhatsApp con Gasbel
                                </a>
                            </div>

                            <!-- Quote Metadata Card -->
                            <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 16px; margin-bottom: 20px; font-size: 13px;"">
                                <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""3"">
                                    <tr>
                                        <td width=""130"" style=""color: #64748b; font-weight: 600;"">Fecha de emisión:</td>
                                        <td style=""color: #0f172a; font-weight: 600;"">{dateFormatted} hs</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600;"">Teléfono registrado:</td>
                                        <td style=""color: #0f172a;"">{WebUtility.HtmlEncode(quote.CustomerPhone)}</td>
                                    </tr>
                                    {(string.IsNullOrWhiteSpace(quote.Notes) ? "" : $@"
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600; vertical-align: top;"">Entrega y notas:</td>
                                        <td style=""color: #0f172a;"">{WebUtility.HtmlEncode(quote.Notes)}</td>
                                    </tr>")}
                                </table>
                            </div>

                            <!-- Products Table -->
                            <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""border-collapse: collapse; margin-bottom: 20px;"">
                                <thead>
                                    <tr style=""background-color: #f1f5f9; border-bottom: 2px solid #e2e8f0;"">
                                        <th align=""left"" style=""padding: 10px; font-size: 11px; font-weight: 700; color: #475569; text-transform: uppercase;"">Detalle</th>
                                        <th align=""center"" style=""padding: 10px; font-size: 11px; font-weight: 700; color: #475569; text-transform: uppercase;"">Presentación</th>
                                        <th align=""center"" style=""padding: 10px; font-size: 11px; font-weight: 700; color: #475569; text-transform: uppercase;"">Cant.</th>
                                        <th align=""right"" style=""padding: 10px; font-size: 11px; font-weight: 700; color: #475569; text-transform: uppercase;"">Precio Unit.</th>
                                        <th align=""right"" style=""padding: 10px; font-size: 11px; font-weight: 700; color: #475569; text-transform: uppercase;"">Total</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {itemsRows}
                                </tbody>
                            </table>

                            <!-- Totals Box -->
                            <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""margin-bottom: 24px;"">
                                <tr>
                                    <td width=""45%""></td>
                                    <td width=""55%"">
                                        <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 14px;"">
                                            <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""4"" style=""font-size: 13px;"">
                                                <tr>
                                                    <td style=""color: #64748b;"">Subtotal:</td>
                                                    <td align=""right"" style=""font-weight: 600; color: #0f172a;"">${quote.Subtotal:N2}</td>
                                                </tr>
                                                <tr>
                                                    <td style=""color: #64748b;"">IVA (21%):</td>
                                                    <td align=""right"" style=""font-weight: 600; color: #0f172a;"">${quote.TaxAmount:N2}</td>
                                                </tr>
                                                <tr style=""border-top: 2px solid #e2e8f0;"">
                                                    <td style=""padding-top: 8px; font-size: 15px; font-weight: 800; color: #0f172a;"">TOTAL:</td>
                                                    <td align=""right"" style=""padding-top: 8px; font-size: 18px; font-weight: 900; color: #d97706;"">${quote.Total:N2}</td>
                                                </tr>
                                            </table>
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- Disclaimer & Contact -->
                            <div style=""background-color: #fffbeb; border: 1px solid #fde68a; border-radius: 10px; padding: 12px; font-size: 11px; color: #92400e; margin-bottom: 20px;"">
                                ⚠️ <strong>Nota:</strong> Este documento es una cotización informativa preliminar. La reserva definitiva de mercadería se confirma al coordinar la entrega con un asesor de ventas.
                            </div>

                            <!-- Footer -->
                            <div style=""text-align: center; border-top: 1px solid #e2e8f0; padding-top: 18px; color: #94a3b8; font-size: 11px;"">
                                <strong>Gasbel Descartables y Perfumería</strong><br>
                                WhatsApp: +54 9 {cleanGasbelPhone} • Mendoza, Argentina<br>
                                Respondé directamente a este correo para comunicarte con nosotros.
                            </div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string CleanPhoneNumber(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone)) return "2613890532";
        var digits = new string(rawPhone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("549")) return digits[3..];
        if (digits.StartsWith("54")) return digits[2..];
        return digits;
    }
}
