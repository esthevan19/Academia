# GymFlow — implantação segura dos treinos

## Antes de publicar
1. Execute backup completo do banco SQL Server em uso e valide que o backup pode ser restaurado.
2. Confirme que a connection string `DefaultConnection` aponta para o banco correto.
3. Em ambiente de homologação com cópia dos dados, aplique a migration `20261117180000_AddStudentWorkouts` e teste cadastro, edição, consulta e exclusão de fichas.
4. Teste a API com JWT para cada perfil: SuperAdmin, Admin, Gerente e Instrutor podem gerenciar; Aluno só pode consultar suas próprias fichas; Recepcionista não tem acesso aos treinos.
5. Confirme que os alunos têm vínculo de Identity com Member e que a claim `gymflow:member_id` está presente no JWT.
6. Verifique a correspondência do modelo EF com o snapshot de migrações antes de gerar novas migrations.

## Publicação
Em produção, as migrations **não** são mais executadas automaticamente sem opt-in. Depois do backup, defina temporariamente a variável `Database__ApplyMigrations=true` e reinicie a aplicação para aplicar as migrations pendentes. Após confirmar o sucesso, remova a variável ou configure `false` e reinicie.

Atenção: se houver migrations anteriores pendentes, o opt-in também tentará aplicá-las. Revise a lista primeiro.

## Testes mínimos de integração
- Gestor cria ficha A para aluno 1 e ficha B para aluno 2; ambas aparecem no MVC.
- Login JWT do aluno 1 retorna somente a ficha A em `GET /api/workouts`, inclusive quando usa `?memberId=2`.
- Login JWT do aluno 2 retorna somente a ficha B.
- Recepcionista recebe acesso negado na API de treinos.
- Instrutor altera séries/carga por `PUT /api/workouts/{id}` e consulta os novos dados.
- Excluir um treino remove também seus exercícios, sem excluir o aluno.
- O aplicativo lê as fichas do usuário autenticado, sem conteúdo de demonstração.

## APK
Somente após publicar e testar o backend: integrar a PR Mobile, validar TypeScript e lint, gerar APK via GitHub Actions/EAS e instalar em Android real. Testar login e navegação de cada perfil.


## Segurança obrigatória antes de usar com clientes reais

- A inicialização do GymFlow **não cria mais contas de demonstração nem utiliza senhas fixas**. Apenas garante a existência dos seis perfis de acesso. Os dados de exemplo de planos e categorias só são carregados em ambiente de desenvolvimento.
- Essa alteração **não remove contas já criadas**. No painel de usuários, revise cada conta antiga de teste, redefina senhas de contas mantidas e desative/exclua contas de demonstração que não são necessárias. Confira principalmente os perfis SuperAdmin, Admin e Gerente. Não compartilhe senhas ou tokens em conversas.
- Confirme que existe pelo menos um SuperAdmin legítimo com acesso testado **antes** de excluir contas antigas. Para um banco novo, providencie um processo seguro de criação do primeiro administrador; não reintroduza contas de senha conhecida no código.
- Mantenha `Database__ApplyMigrations=false` até verificar o histórico `__EFMigrationsHistory` e o backup em homologação. A ausência das tabelas de treinos não é resolvida por uma nova publicação do MVC.
