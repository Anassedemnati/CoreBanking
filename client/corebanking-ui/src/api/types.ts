// ─── Clients ────────────────────────────────────────────────────────────────

export interface ClientDto {
  id: string;
  displayName: string;
  externalId: string | null;
  status: 'Pending' | 'Active';
  activationDate: string | null;
}

export interface RegisterClientRequest {
  displayName: string;
  externalId?: string;
}

// ─── Savings Products ────────────────────────────────────────────────────────

export interface SavingsProductDto {
  id: string;
  name: string;
  shortName: string;
  currencyCode: string;
  currencyDecimalPlaces: number;
  nominalAnnualRate: number;
  status: string;
}

export interface CreateSavingsProductRequest {
  name: string;
  shortName: string;
  currencyCode: string;
  currencyDecimalPlaces: number;
  nominalAnnualRate: number;
  compoundingPeriod: number;
  postingPeriod: number;
  calculationType: number;
  daysInYearType: number;
}

// ─── Savings Accounts ────────────────────────────────────────────────────────

export interface SavingsAccountDto {
  id: string;
  accountNo: string;
  clientId: string;
  productId: string;
  status: string;
  currencyCode: string;
  nominalAnnualRate: number;
  submittedOn: string;
  approvedOn: string | null;
  activatedOn: string | null;
  rejectedOn: string | null;
  withdrawnOn: string | null;
  accountBalance: number;
  interestPostedTillDate: string | null;
  closedOn: string | null;
}

export interface SubmitAccountRequest {
  clientId: string;
  productId: string;
  accountNo: string;
  currencyCode: string;
  currencyDecimalPlaces: number;
  nominalAnnualRate: number;
  submittedOn: string;
  compounding?: number;
  postingPeriod?: number;
  daysInYear?: number;
}

export interface SavingsTransactionDto {
  id: string;
  typeId: number;
  type: string;
  transactionDate: string;
  amount: number;
  runningBalance: number;
}

// ─── Numeric Enum Options ────────────────────────────────────────────────────

export const COMPOUNDING_OPTIONS = [
  { value: 1, label: 'Daily' },
  { value: 4, label: 'Monthly' },
] as const;

export const POSTING_PERIOD_OPTIONS = [
  { value: 4, label: 'Monthly' },
  { value: 5, label: 'Quarterly' },
  { value: 6, label: 'Biannual' },
  { value: 7, label: 'Annual' },
] as const;

export const CALCULATION_TYPE_OPTIONS = [
  { value: 1, label: 'Daily Balance' },
  { value: 2, label: 'Average Daily Balance' },
] as const;

export const DAYS_IN_YEAR_OPTIONS = [
  { value: 360, label: '360 Days' },
  { value: 365, label: '365 Days' },
] as const;

// ─── Account Status ──────────────────────────────────────────────────────────

export const ACCOUNT_STATUS_LABELS: Record<string, string> = {
  Submitted: 'Submitted',
  Approved: 'Approved',
  Active: 'Active',
  Withdrawn: 'Withdrawn',
  Rejected: 'Rejected',
  Closed: 'Closed',
};

export const TRANSACTION_TYPE_LABELS: Record<number, string> = {
  1: 'Deposit',
  2: 'Withdrawal',
  3: 'Interest Posting',
};
