import { defineMessages } from "../translate";

// App header (AppShell): navigation per role, account area and the past-due billing banner.
export const shellMessages = defineMessages(
  {
    navDashboard: "Dashboard",
    navProperties: "Properties",
    navNewInspection: "New inspection",
    navAvailable: "Available",
    navMyInspections: "My inspections",
    navCompleted: "Completed",
    navSettings: "Settings",
    roleCompany: "Company",
    roleAgent: "Inspector",
    roleTenant: "Tenant",
    signOut: "Sign out",
    pastDueWarning: "Your last payment failed. Access continues until {date} — update your payment method with your payment provider to avoid interruption.",
  },
  {
    navDashboard: "Painel",
    navProperties: "Imóveis",
    navNewInspection: "Nova vistoria",
    navAvailable: "Disponíveis",
    navMyInspections: "Minhas vistorias",
    navCompleted: "Concluídas",
    navSettings: "Configurações",
    roleCompany: "Empresa",
    roleAgent: "Vistoriador",
    roleTenant: "Inquilino",
    signOut: "Sair",
    pastDueWarning: "Seu último pagamento falhou. O acesso continua até {date} — atualize sua forma de pagamento junto ao provedor para evitar interrupção.",
  },
);
