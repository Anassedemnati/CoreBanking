export const ROLES = {
  TELLER: 'teller',
  BRANCH_MANAGER: 'branch-manager',
  OPS_OFFICER: 'ops-officer',
  HEAD_OFFICE_ADMIN: 'head-office-admin',
  AUDITOR: 'auditor',
} as const;

export type Role = (typeof ROLES)[keyof typeof ROLES];

export const ROLE_LABELS: Record<Role, string> = {
  teller: 'Teller',
  'branch-manager': 'Branch Manager',
  'ops-officer': 'Operations Officer',
  'head-office-admin': 'Head Office Admin',
  auditor: 'Auditor',
};

/** Actions and which roles may perform them */
export const CAN: Record<string, readonly Role[]> = {
  registerClient: [ROLES.TELLER, ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
  activateClient: [ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
  createProduct: [ROLES.OPS_OFFICER, ROLES.HEAD_OFFICE_ADMIN],
  submitAccount: [ROLES.TELLER, ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
  approveAccount: [ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
  rejectAccount: [ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
  activateAccount: [ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
  withdrawAccount: [ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
  closeAccount: [ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
  deposit: [ROLES.TELLER, ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
  withdraw: [ROLES.TELLER, ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
  postInterest: [ROLES.BRANCH_MANAGER, ROLES.HEAD_OFFICE_ADMIN],
} as const;
