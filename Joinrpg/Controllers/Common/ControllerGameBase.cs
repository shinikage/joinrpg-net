﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers.Common
{
  public class ControllerGameBase : ControllerBase
  {
    protected IProjectService ProjectService { get; }
    protected IProjectRepository ProjectRepository { get; }

    protected ControllerGameBase(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService) : base(userManager)
    {
      ProjectRepository = projectRepository;
      ProjectService = projectService;
    }

    protected ActionResult WithProject(int projectId, Func<Project, ProjectAcl, ActionResult> action)
    {
      var project = ProjectRepository.GetProject(projectId);
      if (project == null)
      {
        return HttpNotFound();
      }
      PrepareMasterMenu(project);
      return action(project, project.GetProjectAcl(CurrentUserIdOrDefault));
    }

    protected ActionResult WithProject(Project project, Func<Project, object> viewModelGenerator)
    {
      if (project == null)
      {
        return HttpNotFound();
      }
      PrepareMasterMenu(project);
      return View(viewModelGenerator(project));
    }

    private void PrepareMasterMenu(Project project)
    {
      var acl = project.GetProjectAcl(CurrentUserIdOrDefault);
      if (acl != null)
      {
        ViewBag.MasterMenu = new MasterMenuViewModel
        {
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName
        };
      }
    }

    protected async Task<ActionResult> WithProjectAsync(int projectId,
      Func<Project, ProjectAcl, Task<ActionResult>> action)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      if (project == null)
      {
        return HttpNotFound();
      }
      var acl = project.GetProjectAcl(CurrentUserIdOrDefault);
      PrepareMasterMenu(project);
      return await action(project, acl);
    }

    protected async Task<ActionResult> WithProjectAsync(int projectId, Func<Project, ProjectAcl, ActionResult> action)
    {
      return await WithProjectAsync(projectId, (project, acl) => Task.FromResult(action(project, acl)));
    }

    protected ActionResult WithProject(int projectId, Func<Project, ActionResult> action)
    {
      return WithProject(projectId, (project, acl) => action(project));
    }

    protected ActionResult WithProjectAsMaster(int projectId, Func<ProjectAcl,bool> requiredRights, Func<Project, ActionResult> action)
    {
      return WithProject(projectId, (project, acl) =>
      {
        if (acl == null || !requiredRights(acl))
        {
          return NoAccesToProjectView(project);
        }
        return action(project);
      });
    }

    private ActionResult NoAccesToProjectView(Project project)
    {
      return View("ErrorNoAccessToProject",
        new ErrorNoAccessToProjectViewModel
        {
          CanGrantAccess = project.ProjectAcls.Where(acl => acl.CanGrantRights).Select(acl => acl.User),
          ProjectId = project.ProjectId,
          ProjectName = project.ProjectName
        });
    }

    protected ActionResult WithProjectAsMaster(int projectId, Func<Project, ActionResult> action)
    {
      return WithProjectAsMaster(projectId, acl => true, action);
    }

    protected Task<ActionResult> WithProjectAsMasterAsync(int projectId, Func<Project, Task<ActionResult>> action)
    {
      return WithProjectAsMasterAsync(projectId, acl => true, action);
    }

    protected Task<ActionResult> WithProjectAsMasterAsync(int projectId, Func<Project, ActionResult> action)
    {
      return WithProjectAsMasterAsync(projectId, acl => true, action);
    }

    protected Task<ActionResult> WithProjectAsMasterAsync(int projectId, Func<ProjectAcl, bool> requiredRights, Func<Project, Task<ActionResult>> action)
    {
      return WithProjectAsync(projectId, async (project, acl) =>
      {
        if (acl == null || !requiredRights(acl))
        {
          return NoAccesToProjectView(project);
        }
        return await action(project);
      });
    }

    private Task<ActionResult> WithProjectAsMasterAsync(int projectId, Func<ProjectAcl, bool> requiredRights, Func<Project, ActionResult> action)
    {
      return WithProjectAsync(projectId, (project, acl) =>
      {
        if (acl == null || !requiredRights(acl))
        {
          return NoAccesToProjectView(project);
        }
        return action(project);
      });
    }

    private ActionResult WithSubEntityAsMaster<TProjectSubEntity>(int projectId, int? fieldId,
      Func<Project, IEnumerable<TProjectSubEntity>> subentitySelector,
      Func<ProjectAcl, bool> requiredRights, Func<Project, TProjectSubEntity, ActionResult> action) where TProjectSubEntity : IProjectSubEntity
    {

      return WithProjectAsMaster(projectId, requiredRights, project => LoadSubEntity(fieldId, subentitySelector, action, project));
    }

    private ActionResult WithSubEntity<TProjectSubEntity>(int projectId, int fieldId,
      Func<Project, IEnumerable<TProjectSubEntity>> subentitySelector,
      Func<Project, TProjectSubEntity, ActionResult> action) where TProjectSubEntity: IProjectSubEntity
    {
      return WithProject(projectId, project => LoadSubEntity(fieldId, subentitySelector, action, project));
    }

    private ActionResult LoadSubEntity<TProjectSubEntity>(int? fieldId,
      Func<Project, IEnumerable<TProjectSubEntity>> subentitySelector,
      Func<Project, TProjectSubEntity, ActionResult> action, Project project)
      where TProjectSubEntity : IProjectSubEntity
    {
      var field = subentitySelector(project).SingleOrDefault(e => e.Id == fieldId);
      return field == null ? HttpNotFound() : action(project, field);
    }

    protected ActionResult WithGroup(int projectId, int groupId, Func<Project, CharacterGroup, ActionResult> action)
    {
      return WithSubEntity(projectId, groupId, project => project.CharacterGroups, action);
    }

    protected ActionResult WithCharacter(int projectId, int characterId, Func<Project, Character, ActionResult> action)
    {
      return WithSubEntity(projectId, characterId, project => project.Characters, action);
    }

    protected ActionResult WithClaim(int projectId, int claimId, Func<Project, Claim, bool, bool, ActionResult> actionResult)
    {
      return WithSubEntity(projectId, claimId, project => project.Claims, (project, claim) =>
      {
        if (!claim.HasAccess(CurrentUserId))
        {
          return NoAccesToProjectView(project);
        }
        return actionResult(project, claim, project.HasAccess(CurrentUserId), claim.PlayerUserId == CurrentUserId);
      });
    }

    protected ActionResult WithClaimAsMaster(int projectId, int claimId, Func<Project, Claim, ActionResult> actionResult)
    {
      return WithClaim(projectId, claimId, (project, claim, hasMasterAccess, isMyClaim) =>
      {
        if (!hasMasterAccess)
        {
          return NoAccesToProjectView(project);
        }
        return actionResult(project, claim);
      });
    }

    protected ActionResult WithMyClaim (int projectId, int claimId, Func<Project, Claim, ActionResult> actionResult)
    {
      return WithClaim(projectId, claimId, (project, claim, hasMasterAccess, isMyClaim) =>
      {
        if (!isMyClaim)
        {
          return NoAccesToProjectView(project);
        }
        return actionResult(project, claim);
      });
    }

    protected ActionResult WithGroupAsMaster(int projectId, int? groupId, Func<Project, CharacterGroup, ActionResult> action)
    {
      return WithSubEntityAsMaster(projectId, groupId, project => project.CharacterGroups, pa => pa.CanChangeFields, action);
    }

    protected ActionResult WithGameFieldAsMaster(int projectId, int fieldId,
      Func<Project, ProjectCharacterField, ActionResult> action)
    {
      return WithSubEntityAsMaster(projectId, fieldId, project => project.AllProjectFields, pa => pa.CanChangeFields, action);
    }

    protected ActionResult RedirectToIndex(Project project)
    {
      return RedirectToAction("Index", "GameGroups",
        new {project.ProjectId, project.CharacterGroups.Single(cg => cg.IsRoot).CharacterGroupId});
    }

    protected static IDictionary<int,string> GetCharacterFieldValuesFromPost(Dictionary<string, string> post)
    {
      var prefix = "field.field_";
      return GetDynamicValuesFromPost(post, prefix);
    }

    protected async Task<ActionResult> AsMaster<TEntity>(TEntity entity, Func<TEntity, Task<ActionResult>> action)
      where TEntity:IProjectSubEntity
    {
      if (entity == null)
      {
        return HttpNotFound();
      }
      if (!entity.Project.HasAccess(CurrentUserId))
      {
        return NoAccesToProjectView(entity.Project);
      }

      return await action(entity);
    }

    protected Task<ActionResult> AsMaster<TEntity>(TEntity folder, Func<TEntity, ActionResult> action)
     where TEntity : IProjectSubEntity
    {
      return AsMaster(folder, f => Task.FromResult(action(f)));
    }
  }
}
