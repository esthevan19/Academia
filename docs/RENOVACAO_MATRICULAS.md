# Renovação de matrículas sem perda de histórico

## Problema
A chave primária anterior de `Memberships` era `(MemberId, PlanId)`.
Ela impedia que um aluno renovasse o **mesmo plano** mesmo após vencer.
A nova migração adiciona um identificador `Id` e preserva os registros antigos,
permitindo múltiplas contratações do mesmo plano em datas distintas.

## Ordem obrigatória na MonsterASP
1. Faça um **backup verificável** do banco SQL `db80215` antes de qualquer migração.
2. Teste a migração `20261119190000_AllowMembershipRenewals` em uma cópia do banco, incluindo alunos com matrículas reais.
3. Confira a contagem de matrículas antes e depois (deve ser idêntica) e a presença dos mesmos `MemberId`, `PlanId`, `StartDate`, `EndDate`.
4. Aplique a migração na produção em uma janela de manutenção, com o sistema sem gravar novas matrículas.
5. Publique o MVC/API compatível imediatamente depois e reinicie a aplicação.
6. Mantenha `Database__ApplyMigrations=false`. Gere e revise o SQL idempotente da migração específica com `dotnet ef migrations script 20261117180000_AddStudentWorkouts 20261119190000_AllowMembershipRenewals --idempotent --project GymManagementDAL --startup-project GymManagementPL` antes de executar a etapa 4. Se a migração anterior estiver pendente no destino, resolva essa diferença em homologação antes de prosseguir.
7. Teste com **dados de homologação**, nunca criando cobranças ou alunos fictícios no banco real.

## Validação sugerida
- Cadastro, histórico e consulta de matrículas antigas sem perda de dados.
- Aluno com plano vencido renovando o mesmo plano.
- Bloqueio de duas matrículas vigentes simultâneas (inclusive em requisições concorrentes).
- Cancelamento conforme permissões: SuperAdmin/Admin/Gerente.
- Atualização simultânea da lista no MVC e no APK via `GET /api/memberships`.

**Atenção:** publicar código com a nova entidade antes da migração pode causar
erro de coluna ausente no SQL Server. Não mesclar nem publicar essa PR isoladamente
até que o backup e a migração tenham sido concluídos.
