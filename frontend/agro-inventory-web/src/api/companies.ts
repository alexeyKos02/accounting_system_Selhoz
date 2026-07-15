import { apiGet, apiPost, apiPut, apiDelete } from './http'
import type {
  CompanyListItemDto, CompanyDto, CreateCompanyRequest, UpdateCompanyRequest,
  MemberDto, AddMemberRequest, UpdateMemberRequest, MemberScopesDto, UpdateScopesRequest,
} from './types'

// Хозяйства (ТЗ §2, §21)
export const companiesApi = {
  list: () => apiGet<CompanyListItemDto[]>('/companies'),
  get: (id: string) => apiGet<CompanyDto>(`/companies/${id}`),
  create: (body: CreateCompanyRequest) => apiPost<CompanyDto>('/companies', body),
  update: (id: string, body: UpdateCompanyRequest) => apiPut<CompanyDto>(`/companies/${id}`, body),

  // Членства и области доступа (ТЗ §3, §6)
  members: (companyId: string) => apiGet<MemberDto[]>(`/companies/${companyId}/members`),
  addMember: (companyId: string, body: AddMemberRequest) =>
    apiPost<MemberDto>(`/companies/${companyId}/members`, body),
  updateMember: (companyId: string, membershipId: string, body: UpdateMemberRequest) =>
    apiPut<MemberDto>(`/companies/${companyId}/members/${membershipId}`, body),
  removeMember: (companyId: string, membershipId: string) =>
    apiDelete<void>(`/companies/${companyId}/members/${membershipId}`),
  getScopes: (companyId: string, membershipId: string) =>
    apiGet<MemberScopesDto>(`/companies/${companyId}/members/${membershipId}/scopes`),
  updateScopes: (companyId: string, membershipId: string, body: UpdateScopesRequest) =>
    apiPut<MemberScopesDto>(`/companies/${companyId}/members/${membershipId}/scopes`, body),
}
