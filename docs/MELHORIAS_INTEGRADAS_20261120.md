# GymFlow — melhorias integradas MVC / Android

## Funcionalidades implementadas sem migração SQL

### Indicadores e alertas da academia
- Dashboard MVC: alerta de matrículas vigentes que vencem nos próximos 7 dias.
- `GET /api/dashboard/overview`: total de alunos, alunos com matrícula ativa, instrutores, aulas do dia, próximas aulas, matrículas vencendo e **até oito vencimentos futuros** para equipe autorizada. Não expor a alunos.
- **Importante:** `financialDataAvailable=false`. Este banco não registra faturas e pagamentos. A API não inventa receita ou status de pagamento.

### Evolução do aluno
- `GET /api/me/progress`: somente JWT de Aluno e conta efetivamente vinculada por claim conferida no SQL.
- Retorna nome, plano vigente, dias restantes, presenças confirmadas em aulas coletivas, aulas presentes nos últimos 30 dias, fichas/exercícios **prescritos** e próxima aula reservada.
- Não confundir **fichas de treino prescritas** com **treinos executados**; não há tabela de execução de exercícios neste esquema.
- Quando a versão antiga do SQL ainda não tem tabela de treinos, a API responde `training.available=false`, com contagens `null`, em vez de 500.

### Presença em aulas coletivas por QR
- `GET /api/class-checkin/mine/{sessionId}`: aluno vinculado e com reserva, somente nos 15 minutos anteriores à aula ou durante ela, recebe QR PNG + código de oito dígitos.
- `GET /api/class-checkin/active-sessions`: somente equipe autorizada, lista aulas em andamento.
- `POST /api/class-checkin/confirm`: equipe confirma código de oito dígitos e marca `Bookings.IsAttended` em SQL, somente durante o horário da aula e com matrícula vigente.
- `/ClassCheckIn/Index`: confirmação no navegador pelo funcionário com antiforgery.
- O código deriva de HMAC SHA-256 com chave separada por propósito, gira a cada 120 segundos, não contém nome nem matrícula e é recusado após registrar presença.
- O app exibe a imagem QR; a equipe pode digitar o código (ou colar o conteúdo do QR). **Leitura automática pela câmera dentro do APK não foi adicionada**; exigiria dependência e teste de permissão/câmera no Android.
- **Não** é controle de entrada física na academia, nem liberação de catraca. O antigo `/api/CheckIn` permanece intencionalmente não implementado para impedir falso acesso.

## Publicação na MonsterASP
1. Faça backup do banco e confira `ConnectionStrings__DefaultConnection` e `Jwt__Key` da hospedagem (não compartilhe valores).
2. Mantenha `ASPNETCORE_ENVIRONMENT=Production` e `Database__ApplyMigrations=false`.
3. Atualize e publique `GymManagementPL` a partir de `main`, usando WebDeploy `site108087-WebDeploy`.
4. Consulte `https://gymflowfit.runasp.net/api/health` e confira `status=ok`, e teste login, cadastro, matrícula, treinos, aulas e navegação MVC.
5. Só então instale o APK atualizado, porque os módulos Android precisam dos endpoints novos.

## Testes e limitações
- Compilação MVC, integração SQL efêmera, fluxo de cadastro, geração de QR, recusa de código inválido, prevenção de replay e presença persistida.
- Permissões em 38 telas MVC.
- Testes de autorização JWT dependem de credenciais reais e devem ser feitos por perfil após a publicação. **Não automatizar escrita no SQL real sem backup e autorização.**
- Financeiro completo, recibos/PIX, registro de treinos concluídos, avaliações com evolução, notificações push e entrada física por QR exigem modelo/infraestrutura próprios, além da correção do histórico de migrações EF Core (issues #35/#38). Não aplicar migração automaticamente.
- Configure opcionalmente `DataProtection__KeysDirectory` em pasta privada persistente e adequada do host para manter as sessões.
- O pacote antigo e o APK antigo não incluem estes recursos. Publicar backend primeiro.

Para validar a presença por QR: crie uma aula com horário real em andamento, faça uma reserva com um Aluno vinculado, entre no APK como aluno e abra **Presença por QR**; na conta Recepcionista/Instrutor abra **Presença por QR**, selecione a aula e confirme os oito dígitos. Verifique a presença na lista de participantes. Testes reais devem ser realizados com cadastros autorizados de homologação ou um banco com backup.
