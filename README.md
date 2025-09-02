# EF Core Ǩ������ (WPF)

һ�������Ŀ������Ŀ�� EF Core Ǩ�ƿ��ӻ�С���ߡ�֧�ֿ����г� DbContext���鿴Ǩ�ơ��ƶϱ�ṹ���������µ�Ǩ�ƣ��Զ�����Ǩ�������ظ���

<image src="..\MigrationTool\MigrationTool\img" alt="Screenshot" width="600"/>

## ��������
- ѡ�� .csproj ���Զ����� TargetFramework/TargetFrameworks
- һ���л�Ŀ���ܣ������Ŀ�꣩
- ö�� DbContext������ `dotnet ef dbcontext list`��
- չʾ����Ǩ���б�ʱ��� - ���ƣ�����������Ŀ¼��
  - `Migrations/`
  - `Migrations/{ContextShort}`��ʾ����`MyDbContext` �� `My`��
- �ƶ�ѡ��Ǩ��֮ǰ�ı�ṹ��Table/Column/Type/Nullable��
- ����Ǩ�ƣ������ `Migrations/{ContextShort}`��������������Ǩ�ƺ�׺������
- ��ϸ���󵯴���������׼���/��������볣��������ʾ

## ����Ҫ��
- Windows
- .NET 8 SDK
- Visual Studio 2022 (��ʹ�� `dotnet build`/`dotnet run`)
- Ŀ����Ŀ������ `Microsoft.EntityFrameworkCore.Design`
- ��װ��ȷ�� `dotnet-ef` ����Ŀ EF Core �汾���ݣ�
  ```bash
  dotnet tool install --global dotnet-ef
  dotnet tool update --global dotnet-ef
  ```

## ʹ��˵��
1. ����Ӧ�á�
2. ��������...����ѡ��Ŀ����Ŀ�� `.csproj` �ļ���
3. �����Ͻ�ѡ��Ŀ���ܣ���Ŀ����Ŀ����ѡ�񣩡�
4. ����ࡰ���ݿ������� (DbContext)����ѡ��һ�������ġ�
5. �Ҳ�ɲ鿴��
   - ���У�����Ǩ�ƣ�ʱ��� - ���ƣ�
   - ���У��ƶϵı�ṹ��Table/Column/Type/Nullable��
6. �����ײ����롰���ݿ�Ǩ�ư汾������ΪǨ�ƺ�׺���������������Ǩ�ơ���
7. ��Ǩ�ƻ����ɵ� `Migrations/{ContextShort}` Ŀ¼�£���ˢ���б�

## ��Ҫ˵��
- ������ͨ���������Ǩ���ļ����ƶϽṹ����Ҫ���ǣ�`CreateTable`��`AddColumn`��`DropColumn`��`AlterColumn`�����ӳ������������������ӱ��ʽ��ԭ�� SQL�������޷�׼ȷʶ�𣬽����ο���
- ����Ǩ������Ŀ����Ŀ�����������Լ��ɹ��������ʱ `DbContext`�����޷���������ȷ����
  - �����޲ι��캯������
  - ʵ�� `IDesignTimeDbContextFactory<TContext>`��
- ��Ŀ����Ŀ����ѡ����� TargetFramework������ `dotnet ef` �����޷�ִ�С�

## ����������
- ʹ�� Visual Studio �򿪽��������ֱ�����У���������������ĿĿ¼ִ�У�
  ```bash
  dotnet build
  ```
- ����Ӧ�ú󰴡�ʹ��˵����������

## Ŀ¼�ṹ���ؼ���
- `MigrationTool/` WPF Ӧ�ù���
  - `MainWindow.xaml` �� `MainWindow.xaml.cs`�����������߼�
  - `MigrationFileEntry.cs`��`MigrationItem.cs`������ģ��

## �����Ų�
- δ�ҵ� `dotnet-ef`����װ/����ȫ�ֹ��߲�ȷ�� PATH��
- �޷����� `DbContext`������޲ι���� `IDesignTimeDbContextFactory<TContext>` ʵ�֡�
- �Ҳ�����Ŀ��·����ȷ�Ϲ���Ŀ¼�� `.csproj` �Ƿ���ȷ��ȷ����Ŀ������������
