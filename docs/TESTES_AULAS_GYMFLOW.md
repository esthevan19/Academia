# GymFlow — roteiro de validação de aulas, reservas e presenças

Esta entrega **não cria nem modifica tabelas**. O site MVC e o aplicativo Android usam as mesmas tabelas `Sessions`, `Bookings`, `Memberships` e os vínculos `AspNetUserClaims`. Mantenha `Database__ApplyMigrations=false` na hospedagem até revisão específica de migrações.

## Antes dos testes

1. Faça backup do banco SQL Server. Prefira um ambiente de homologação com dados fictícios; não crie reservas fictícias em aulas reais de clientes.
2. Publique o projeto `GymManagementPL` no site MonsterASP.NET usando o perfil WebDeploy já configurado.
3. Entre novamente no site após a publicação para atualizar o cookie. No APK, saia e entre novamente para atualizar o JWT.
4. Prepare duas contas distintas de Aluno, dois cadastros de aluno e pelo menos uma matrícula ativa. Vincule cada conta ao cadastro correspondente no painel SuperAdmin.
5. Cadastre uma aula futura com professor, categoria, capacidade pequena e horário local de Brasília.

## Matriz de testes

| Perfil | Ação | Resultado esperado |
|---|---|---|
| SuperAdmin | Vincular uma conta de Aluno a um cadastro | Vínculo salvo; outra conta não pode usar o mesmo cadastro |
| Admin/Gerente | Criar uma aula futura | Aula aparece em **Aulas coletivas** e na **Agenda** |
| Recepcionista | Reservar vaga para aluno com matrícula ativa | Participante aparece na listagem e quantidade de vagas diminui |
| Recepcionista | Tentar reservar para aluno sem matrícula | Sistema impede a operação e exibe mensagem em português |
| Aluno vinculado | Abrir **Minhas aulas** | Exibe as aulas futuras e apenas suas próprias reservas |
| Aluno vinculado | Reservar vaga | Reserva é salva e permanece após atualizar a página |
| Aluno vinculado | Cancelar antes do início | Reserva some da lista e a vaga volta a ficar disponível |
| Aluno sem vínculo | Abrir **Minhas aulas** | Orientação para solicitar vínculo, sem exibir reservas de terceiros |
| Aluno sem matrícula ativa | Abrir **Minhas aulas** | Aulas visíveis, botão de reserva bloqueado e orientação para regularizar |
| Instrutor | Abrir uma aula em andamento e confirmar presença | Participante aparece como **Presente** |
| Instrutor | Tentar registrar presença fora do horário | Operação bloqueada no servidor |
| Qualquer perfil sem permissão | Abrir URL de alteração de outro perfil | Acesso negado |
| Aluno com vínculo removido | Tentar abrir **Meus treinos** e **Minhas aulas** sem sair da conta | Dados antigos não são mostrados |
| Aluno no APK | Agendar/cancelar em **Agenda** | As alterações aparecem no MVC após atualizar |
| Instrutor no APK | Registrar presença durante a aula | Presença aparece no MVC |

## Verificações importantes

- Usar o formato **DD/MM/AAAA** e horários no fuso de Brasília.
- Não ultrapassar a capacidade de participantes, não permitir reservas duplicadas e não permitir cancelamento após o início.
- Verificar que formulários inválidos preservam os dados digitados e mostram a mensagem adequada.
- Conferir visual em computador e celular, inclusive menu e botões.
- Verificar que não há erro HTTP 500 e que os dados permanecem após sair/entrar novamente.
- Caso haja falha, verificar logs da MonsterASP e respostas HTTP da API; não executar scripts SQL aleatórios em produção.

## Compilação e publicação

```powershell
Set-Location "C:\Users\esthe\source\repos\Academia"
git switch main
git pull --ff-only origin main
dotnet build GymManagementSystemSolution.slnx -c Release
```

Depois publique `GymManagementPL` pelo Visual Studio. Para gerar um novo APK após publicar a API, use o workflow **Build Android APK** no repositório `esthevan19/GymFlow-Mobile`, branch `main`, com `EXPO_TOKEN` válido.

**Importante:** compilação aprovada no GitHub valida o código, mas não substitui os testes de ponta a ponta contra o servidor e em dispositivo Android.
