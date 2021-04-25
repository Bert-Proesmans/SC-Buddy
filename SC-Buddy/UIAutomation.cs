using SC_Buddy.Helpers;
using SC_Buddy.Model;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;

namespace SC_Buddy
{
    public enum DirectionOfValuta { Left, Right };
    public record SuppaChatto(Rect? BoundingBox, DirectionOfValuta? ValutaDirection, Simp DollaDollaBill);
    public record Simp(ISOCurrency? Currency, decimal? Amount, string? Name, string? Message);

    public class UIAutomation : IDisposable
    {
        record AutomationDetails(
            // The element holding the superchat amount
            AutomationElement? Target,
            // The element targetted by the user
            // ERROR; THIS ELEMENT MUST KEEP REFERENCE TO THE UI ELEMENT! Don't aggresively cache this element
            AutomationElement Origin,
            // All sibling grid cell elements from the point of origin, if found
            AutomationElementCollection? RowCellElements,
            // The parsed data
            SuppaChatto? Result);

        private readonly Subject<Point> _ingress;
        private bool disposedValue;

        public UIAutomation()
        {
            _ingress = new Subject<Point>();

            Egress = ((IObservable<Point>)_ingress)
                .ObserveOn(NewThreadScheduler.Default)
                // .Trace(nameof(FormulateElement))
                .Select(FormulateElement)
                .Where(x => x != null)
                .Select(x => x!)
                .Select(ProvisionMetaData)
                .Select(StraightUpFindAmount)
                .Select(FindTargetInRow)
                .Select(LocateMetadata)
                // ___
                .Select(x => x.Result ?? new SuppaChatto(default, default, new(default, default, default, default)))
                .Publish()
                .RefCount();
        }

        public IObserver<Point> Ingress => _ingress;

        public IObservable<SuppaChatto> Egress { get; }

        static AutomationDetails? FormulateElement(Point p)
        {
            // NOTE; Cache data to speed up stuffs
            var wideCaching = new CacheRequest();
            wideCaching.Add(AutomationElement.ControlTypeProperty);
            wideCaching.Add(AutomationElement.NameProperty);
            wideCaching.Add(AutomationElement.BoundingRectangleProperty);
            wideCaching.Add(ValuePattern.Pattern);
            wideCaching.Add(ValuePattern.ValueProperty);
            wideCaching.Add(GridPattern.Pattern);
            wideCaching.Add(GridItemPattern.Pattern);
            wideCaching.Add(GridItemPattern.ColumnProperty);
            wideCaching.Add(GridItemPattern.RowProperty);
            wideCaching.Add(GridItemPattern.ContainingGridProperty);
            wideCaching.Add(TableItemPattern.Pattern);
            wideCaching.Add(TableItemPattern.ColumnHeaderItemsProperty);

            AutomationElement? pointedElement;
            try
            {
                using (wideCaching.Activate())
                {
                    pointedElement = AutomationElement.FromPoint(new Point(p.X, p.Y));

                    // WARN; The cache request DOES NOT handle each recursively encountered element!
                    // eg; GridItemPattern.ContainingGrid -> AutomationElement -> !GridPattern
                }
            }
            catch (COMException)
            {
                // NOTE; Inconsistent internal state.
                // WARN; Hope for automatic recovery!
                return null;
            }
            catch (ArgumentException)
            {
                // NOTE; High chance of elevated, unaccessible, UI element
                return null;
            }
            catch (ElementNotAvailableException)
            {
                // NOTE; Element dissapeared when loading properties
                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Unrecognized exception ({e.GetType().FullName}) occurred during automation element retrievel: {e.Message}");
                return null;
            }

            var controlTypeName = pointedElement.Cached.ControlType.LocalizedControlType;
            var controlTypeId = pointedElement.Cached.ControlType.Id.ToString("X4");
            Debug.WriteLine($"Target control type: {controlTypeName} ({controlTypeId})");

            var result = new SuppaChatto(default, default, new(default, default, default, default));
            // WARN; Keep reference to cache and other elements alive to not get into Undefined Behaviour.
            return new AutomationDetails(default, pointedElement, default, result);
        }

        static AutomationDetails ProvisionMetaData(AutomationDetails state)
        {
            if (state.RowCellElements != null) return state;

            var gridItemCaching = new CacheRequest();
            gridItemCaching.Add(AutomationElement.NameProperty);
            gridItemCaching.Add(AutomationElement.BoundingRectangleProperty);
            gridItemCaching.Add(GridItemPattern.Pattern);
            gridItemCaching.Add(GridItemPattern.ColumnProperty);
            gridItemCaching.Add(GridItemPattern.RowProperty);
            gridItemCaching.Add(ValuePattern.Pattern);
            gridItemCaching.Add(ValuePattern.ValueProperty);
            // ERROR; Don't detach from UI because we will replace the origin element with the
            // one that has GridItemPattern enabled!
            // gridItemCaching.AutomationElementMode = AutomationElementMode.None;

            var gridItemElement = GetGridItemElement(state.Origin, gridItemCaching);
            if (gridItemElement == null) return state;
            Debug.WriteLine($"Using GridItemElement {gridItemElement.Cached.Name}");

            var originGridItem = (GridItemPattern)gridItemElement.GetCachedPattern(GridItemPattern.Pattern);
            var originGridRow = originGridItem.Cached.Row;
            var oldParentGrid = originGridItem.Cached.ContainingGrid;
            if (oldParentGrid == null) return state;

            // ERROR; Some weird error happens when searching descendants AFTER refreshing cashed data.
            // This probably has to do with multiple managed .net objects pointing to the same UIAutomation
            // COM-object with incorrect aliveness tracking.
            var gridRowCellElements = GetGridRowCellElements(oldParentGrid, originGridRow, gridItemCaching);
            if (gridRowCellElements.Count == 0) return state;

            // NOTE; Origin element is overwritten!
            var newOriginElement = gridItemElement ?? state.Origin;
            return new(state.Target, newOriginElement, gridRowCellElements, state.Result);
        }

        static AutomationDetails StraightUpFindAmount(AutomationDetails state)
        {
            if (state.Result?.DollaDollaBill?.Currency != default ||
                state.Result?.DollaDollaBill?.Amount != default) return state;

            var potentialTarget = state.Target ?? state.Origin;
            var (Currency, Amount) = DetectMonetaryAmount(potentialTarget);

            // CASE; No currency or amount detected, no fallback left
            if (Currency == null || Amount == null)
            {
                return state;
            }

            // TODO; Introduce fricking lenses, djeezus
            var old = state.Result;
            var bb = old?.BoundingBox ?? potentialTarget?.Cached.BoundingRectangle;
            var ddb = new Simp(Currency, Amount, old?.DollaDollaBill?.Message ?? default, old?.DollaDollaBill?.Name ?? default);
            var newResult = new SuppaChatto(bb, old?.ValutaDirection, ddb);
            return new(potentialTarget, state.Origin, state.RowCellElements, newResult);
        }

        static AutomationDetails FindTargetInRow(AutomationDetails state)
        {
            if (state.Target != null ||
                state.RowCellElements == null) return state;

            var gridCaching = new CacheRequest();
            gridCaching.Add(GridPattern.Pattern);
            gridCaching.Add(GridPattern.ColumnCountProperty);
            gridCaching.AutomationElementMode = AutomationElementMode.None;

            int? originGridColumn = null;
            if (state.Origin.TryGetCachedPattern(GridItemPattern.Pattern, out object gridItemObject))
            {
                originGridColumn = ((GridItemPattern)gridItemObject).Cached.Column;
            }

            var (PotentialTarget, Currency, Amount) = DetectMonetaryAmountInCellElements(state.RowCellElements, (originGridColumn ?? int.MaxValue));
            // CASE; No currency or amount detected, no fallback left
            if (PotentialTarget == null || Currency == null || Amount == null)
            {
                return state;
            }

            DirectionOfValuta? valutaDirection;
            var targetGridColumn = ((GridItemPattern)PotentialTarget.GetCachedPattern(GridItemPattern.Pattern)).Cached.Column;
            if (originGridColumn.HasValue && targetGridColumn < originGridColumn) valutaDirection = DirectionOfValuta.Left;
            else if (originGridColumn.HasValue && targetGridColumn > originGridColumn) valutaDirection = DirectionOfValuta.Right;
            else valutaDirection = null;

            // TODO; Introduce fricking lenses, djeezus
            var old = state.Result;
            var bb = old?.BoundingBox ?? PotentialTarget?.Cached.BoundingRectangle;
            var vd = old?.ValutaDirection ?? valutaDirection;
            var ddb = new Simp(Currency, Amount, old?.DollaDollaBill?.Message, old?.DollaDollaBill?.Name);
            var newResult = new SuppaChatto(bb, vd, ddb);
            return new(PotentialTarget, state.Origin, state.RowCellElements, newResult);
        }

        static AutomationDetails LocateMetadata(AutomationDetails state)
        {
            if (state.Target == null ||
                state.RowCellElements == null) return state;

            var scAmountIdx = ((GridItemPattern)state.Target.GetCachedPattern(GridItemPattern.Pattern)).Cached.Column;
            var sbTextBefore = new StringBuilder();
            var sbTextAfter = new StringBuilder();

            var items = state
                .RowCellElements
                .Cast<AutomationElement>()
                .Select((element, idx) => (element, idx))
                .Where(x => x.idx != scAmountIdx);
            foreach (var (element, idx) in items)
            {
                var sb = (idx < scAmountIdx) ? sbTextBefore : sbTextAfter;
                if (element == null) continue;

                var elementName = element.Cached.Name;
                sb.AppendLine(elementName);
            }

            var nameText = sbTextBefore.ToString();
            var messageText = sbTextAfter.ToString();

            // TODO; Introduce fricking lenses, djeezus
            var old = state.Result;
            var message = old?.DollaDollaBill?.Message ?? messageText;
            var name = old?.DollaDollaBill?.Name ?? nameText;
            var ddb = new Simp(old?.DollaDollaBill?.Currency, old?.DollaDollaBill?.Amount, name, message);
            var newResult = new SuppaChatto(old?.BoundingBox, old?.ValutaDirection, ddb);
            return new(state.Target, state.Origin, state.RowCellElements, newResult);
        }

        static (ISOCurrency?, decimal?) DetectMonetaryAmount(AutomationElement potentialTarget)
        {
            ISOCurrency? Currency = null;
            decimal? Amount = null;

            // NOTE; Attempt 1, use value pattern
            if (potentialTarget.TryGetCachedPattern(ValuePattern.Pattern, out object valueObject))
            {
                var valuePattern = (ValuePattern)valueObject;
                // WARN; It doesn't seem that we can get the actual selected text to further slice the amount of
                // possibilities to transmogrify.
                var value = valuePattern.Cached.Value;
                (Currency, Amount) = Currencies.ExtractCurrency(value);
            }

            // NOTE; Attempt 2, use name of element
            if (Currency == null || Amount == null)
            {
                (Currency, Amount) = Currencies.ExtractCurrency(potentialTarget.Cached.Name);
            }

            // CASE; No currency or amount detected, no fallback left
            if (Currency == null || Amount == null)
            {
                // Do nothing
            }

            return (Currency, Amount);
        }

        static AutomationElement? GetGridItemElement(AutomationElement target, CacheRequest cacheRequest)
        {
            const int maximumParentJumps = 2;
            if (target.TryGetCachedPattern(GridItemPattern.Pattern, out object _) == false)
            {
                // NOTE; According to the spec a TableItemPattern is always accompanied by a
                // GridItemPattern implementation.
                var gridItemCondition = new PropertyCondition(AutomationElement.IsGridItemPatternAvailableProperty, true);
                var tableItemCondition = new PropertyCondition(AutomationElement.IsTableItemPatternAvailableProperty, true);
                var condition = new OrCondition(gridItemCondition, tableItemCondition);
                return AutomationHelper.GetFirstParentElement(target, condition, cacheRequest, maximumParentJumps);
            }

            return target;
        }

        static AutomationElementCollection GetGridRowCellElements(AutomationElement grid, int row, CacheRequest cacheRequest)
        {
            using (cacheRequest.Activate())
            {
                var isGridItem = new PropertyCondition(AutomationElement.IsGridItemPatternAvailableProperty, true);
                var isGridRow = new PropertyCondition(GridItemPattern.RowProperty, row);
                // ERROR; For some reason refreshing the cache of the parentGrid BEFORE doing a subtree search!
                return grid.FindAll(TreeScope.Descendants, new AndCondition(isGridItem, isGridRow));
            }
        }

        static (AutomationElement?, ISOCurrency?, decimal?) DetectMonetaryAmountInCellElements(AutomationElementCollection cellElements, params int[] skipIdxs)
        {
            if (cellElements.Count == 0) return (default, default, default);

            var items = cellElements.Cast<AutomationElement>().Select((element, idx) => (element, idx));
            var firstItem = items.FirstOrDefault(x => skipIdxs.Contains(x.idx) == false).element;

            if (firstItem != null && firstItem.TryGetCachedPattern(TableItemPattern.Pattern, out object tableItemObject))
            {
                var tableItem = (TableItemPattern)tableItemObject;
                // TODO; Use header info to O(1) find the correct cell and value.
            }

            foreach (var (cell, _) in items)
            {
                if (cell == null) continue;

                var potential = DetectMonetaryAmount(cell);
                if (potential.Item1 != null && potential.Item2 != null)
                {
                    var (Currency, Amount) = potential;
                    return (cell, Currency, Amount);
                }
            }

            return (default, default, default);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _ingress.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
