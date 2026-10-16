namespace Gasbel.Domain.Entities;

public class CommercialSettings
{
    public decimal MinPurchaseForDiscount { get; set; } = 50000m;
    public decimal MinPurchaseForFreeShipping { get; set; } = 100000m;
    public string WhatsAppProduction { get; set; } = "2616799084";
    public string WhatsAppTesting { get; set; } = "2613890532";
    public bool UseTestingNumber { get; set; } = true;
    public string FreeShippingLegendPrefix { get; set; } = "El envío es gratis a partir de ";

    // Configuraciones de Notificaciones por Email
    public string NotificationEmail { get; set; } = "pedidos@gasbel.com.ar";
    public string SenderEmail { get; set; } = "cotizaciones@gasbel.com.ar";
    public string SenderName { get; set; } = "Gasbel Descartables y Perfumería";
    public bool SendEmailToCustomer { get; set; } = true;
    public bool SendEmailToBusiness { get; set; } = true;
    public string ResendApiKey { get; set; } = "";
    public string EmailProvider { get; set; } = "Resend"; // Resend, Smtp, Simulated
    
    // Opciones SMTP de respaldo
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public bool SmtpUseSsl { get; set; } = true;
}

