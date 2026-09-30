import { defineMessages } from "../translate";

// Labels for API enum values (inspection types/statuses, room types, comparison classes, ...).
// Keys are the exact values the API returns; values the catalog does not know are split on camel case.
export const enumMessages = defineMessages(
  {
    MoveIn: "Move In", MoveOut: "Move Out", Periodic: "Periodic", Other: "Other",
    Draft: "Draft", Open: "Open", Assigned: "Assigned", InProgress: "In progress", Review: "In review",
    Completed: "Completed", AwaitingTenant: "Awaiting tenant", Accepted: "Accepted", Disputed: "Disputed",
    Cancelled: "Cancelled", Expired: "Expired",
    Public: "Public", Private: "Private",
    LivingRoom: "Living room", DiningRoom: "Dining room", Bedroom: "Bedroom", Kitchen: "Kitchen", Bathroom: "Bathroom",
    Hallway: "Hallway", Garage: "Garage", Garden: "Garden", Utility: "Utility", Office: "Office", Balcony: "Balcony", Exterior: "Exterior",
    House: "House", Apartment: "Apartment", Studio: "Studio", Townhouse: "Townhouse", Bungalow: "Bungalow", Commercial: "Commercial",
    Unknown: "Unknown",
    NewDamage: "New damage", PreExisting: "Pre-existing", NormalWear: "Normal wear", Resolved: "Resolved",
    Unchanged: "Unchanged", UnableToDetermine: "Unable to determine",
    Pending: "Pending", Processing: "Processing", Failed: "Failed",
    Active: "Active", PastDue: "Past due", None: "None",
    Month: "month", Year: "year",
    Good: "Good", Fair: "Fair", Poor: "Poor",
  },
  {
    MoveIn: "Entrada", MoveOut: "Saída", Periodic: "Periódica", Other: "Outra",
    Draft: "Rascunho", Open: "Aberta", Assigned: "Atribuída", InProgress: "Em andamento", Review: "Em revisão",
    Completed: "Concluída", AwaitingTenant: "Aguardando inquilino", Accepted: "Aceita", Disputed: "Contestada",
    Cancelled: "Cancelada", Expired: "Expirada",
    Public: "Pública", Private: "Privada",
    LivingRoom: "Sala de estar", DiningRoom: "Sala de jantar", Bedroom: "Quarto", Kitchen: "Cozinha", Bathroom: "Banheiro",
    Hallway: "Corredor", Garage: "Garagem", Garden: "Jardim", Utility: "Área de serviço", Office: "Escritório", Balcony: "Varanda", Exterior: "Área externa",
    House: "Casa", Apartment: "Apartamento", Studio: "Studio", Townhouse: "Sobrado", Bungalow: "Casa térrea", Commercial: "Comercial",
    Unknown: "Desconhecido",
    NewDamage: "Dano novo", PreExisting: "Pré-existente", NormalWear: "Desgaste natural", Resolved: "Resolvido",
    Unchanged: "Sem alteração", UnableToDetermine: "Não foi possível determinar",
    Pending: "Pendente", Processing: "Processando", Failed: "Falhou",
    Active: "Ativa", PastDue: "Pagamento atrasado", None: "Nenhuma",
    Month: "mês", Year: "ano",
    Good: "Bom", Fair: "Regular", Poor: "Ruim",
  },
);
