﻿using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (used by LINQ)
  public class Project : IProjectEntity
  {
    public int ProjectId { get; set; }

    int IOrderableEntity.Id => ProjectId;

    public string ProjectName { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<ProjectAcl> ProjectAcls { get; set; }

    public virtual ICollection<ProjectCharacterField> ProjectFields { get; set; }

    public virtual ICollection<CharacterGroup>  CharacterGroups { get; set; }

    public virtual ICollection<Character>  Characters { get; set; }

    public virtual ICollection<Claim> Claims { get; set; }

    public virtual ProjectDetails Details { get; set; }

    public virtual ICollection<PlotFolder> PlotFolders { get; set; }

    Project IProjectEntity.Project => this;

    public virtual ICollection<ProjectFeeSetting>  ProjectFeeSettings { get; set; }
    public virtual ICollection<PaymentType> PaymentTypes { get; set; }

    #region helper properties
    public IEnumerable<PaymentType> ActivePaymentTypes => PaymentTypes.Where(pt => pt.IsActive);

    public IEnumerable<ProjectCharacterField> ActiveProjectFields
      => ProjectFields.Where(pf => pf.IsActive).OrderBy(pf => pf.Order);

    public IEnumerable<ProjectCharacterField> AllProjectFields
      => ProjectFields.OrderByDescending(pf => pf.IsActive).ThenBy(pf => pf.Order);

    public CharacterGroup RootGroup => CharacterGroups.Single(g => g.IsRoot);
    #endregion
  }

  public class ProjectDetails
  {
    public int ProjectId { get; set; }
    public MarkdownString ClaimApplyRules { get; set; } = new MarkdownString();
    public MarkdownString ProjectAnnounce { get; set; } = new MarkdownString();
    public int? AllrpgId { get; set; }
  }

  public class ProjectFeeSetting
  {
    public int ProjectFeeSettingId { get; set; }
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }
    public int Fee { get; set; }
    public DateTime EndDate { get; set; }
  }
}
